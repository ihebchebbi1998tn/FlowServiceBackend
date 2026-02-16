using MyApi.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyApi.Modules.Auth.Controllers
{
    /// <summary>
    /// Handles OAuth provider redirects (GET requests from Google/Microsoft).
    /// Google redirects here with ?code=...&state=... after user consent.
    /// This controller exchanges the code for tokens, resolves the user,
    /// and redirects back to the frontend with auth data.
    /// </summary>
    [ApiController]
    [Route("oauth")]
    [AllowAnonymous]
    public class OAuthCallbackController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OAuthCallbackController> _logger;

        // Default frontend origin — override via env var FRONTEND_ORIGIN
        private string FrontendOrigin =>
            Environment.GetEnvironmentVariable("FRONTEND_ORIGIN")
            ?? _configuration["Frontend:Origin"]
            ?? "https://flowentra.app";

        public OAuthCallbackController(
            IAuthService authService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<OAuthCallbackController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// GET /oauth/google/callback?code=...&state=...&scope=...
        /// Called by Google after user grants consent.
        /// Exchanges code → tokens → email, then logs user in and redirects to frontend.
        /// </summary>
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error)
        {
            // ── Handle denial / errors from Google ──
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Google OAuth error: {Error}", error);
                return Redirect($"{FrontendOrigin}/login?oauth_error={Uri.EscapeDataString(error)}");
            }

            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Google OAuth callback received without code");
                return Redirect($"{FrontendOrigin}/login?oauth_error=missing_code");
            }

            try
            {
                // ── 1. Exchange authorization code for Google tokens ──
                var googleSection = _configuration.GetSection("OAuth:Google");
                var clientId = googleSection["ClientId"];
                var clientSecret = googleSection["ClientSecret"];
                var redirectUri = googleSection["RedirectUri"]
                    ?? $"{Request.Scheme}://{Request.Host}/oauth/google/callback";

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("Google OAuth is not configured (missing ClientId/ClientSecret)");
                    return Redirect($"{FrontendOrigin}/login?oauth_error=provider_not_configured");
                }

                var tokenResponse = await ExchangeGoogleCodeAsync(code, clientId, clientSecret, redirectUri);

                if (tokenResponse == null)
                {
                    return Redirect($"{FrontendOrigin}/login?oauth_error=token_exchange_failed");
                }

                // ── 2. Get user info from Google ──
                var userInfo = await GetGoogleUserInfoAsync(tokenResponse.AccessToken);

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogWarning("Could not retrieve email from Google user info");
                    return Redirect($"{FrontendOrigin}/login?oauth_error=email_not_found");
                }

                _logger.LogInformation("Google OAuth callback for email: {Email}", userInfo.Email);

                // ── 3. Authenticate via existing OAuthLoginAsync ──
                var authResponse = await _authService.OAuthLoginAsync(userInfo.Email);

                if (!authResponse.Success)
                {
                    // User doesn't exist yet — redirect to signup with pre-filled data
                    var signupParams = $"oauth_provider=google"
                        + $"&email={Uri.EscapeDataString(userInfo.Email)}"
                        + $"&first_name={Uri.EscapeDataString(userInfo.FirstName ?? "")}"
                        + $"&last_name={Uri.EscapeDataString(userInfo.LastName ?? "")}"
                        + $"&profile_picture={Uri.EscapeDataString(userInfo.Picture ?? "")}";

                    return Redirect($"{FrontendOrigin}/signup?{signupParams}");
                }

                // ── 4. Redirect to frontend with tokens ──
                // Using fragment (#) so tokens aren't sent to server in referer headers
                var fragment = $"access_token={Uri.EscapeDataString(authResponse.AccessToken ?? "")}"
                    + $"&refresh_token={Uri.EscapeDataString(authResponse.RefreshToken ?? "")}"
                    + $"&expires_at={Uri.EscapeDataString(authResponse.ExpiresAt?.ToString("o") ?? "")}"
                    + $"&user_id={authResponse.User?.Id}"
                    + $"&email={Uri.EscapeDataString(authResponse.User?.Email ?? "")}";

                return Redirect($"{FrontendOrigin}/oauth/callback#{fragment}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Google OAuth callback");
                return Redirect($"{FrontendOrigin}/login?oauth_error=internal_error");
            }
        }

        // ─── Google Token Exchange ───────────────────────────────────────────

        private async Task<GoogleTokenResponse?> ExchangeGoogleCodeAsync(
            string code, string clientId, string clientSecret, string redirectUri)
        {
            var client = _httpClientFactory.CreateClient();

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", body);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google token exchange failed: {Status} {Body}", response.StatusCode, json);
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
            return new GoogleTokenResponse
            {
                AccessToken = tokenData.GetProperty("access_token").GetString() ?? "",
                RefreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                IdToken = tokenData.TryGetProperty("id_token", out var idt) ? idt.GetString() : null,
            };
        }

        // ─── Google User Info ────────────────────────────────────────────────

        private async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Google user info: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new GoogleUserInfo
            {
                Email = data.TryGetProperty("email", out var e) ? e.GetString() : null,
                FirstName = data.TryGetProperty("given_name", out var fn) ? fn.GetString() : null,
                LastName = data.TryGetProperty("family_name", out var ln) ? ln.GetString() : null,
                Picture = data.TryGetProperty("picture", out var pic) ? pic.GetString() : null,
            };
        }

        // ─── Internal DTOs ───────────────────────────────────────────────────

        private class GoogleTokenResponse
        {
            public string AccessToken { get; set; } = "";
            public string? RefreshToken { get; set; }
            public string? IdToken { get; set; }
        }

        private class GoogleUserInfo
        {
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Picture { get; set; }
        }
    }
}
