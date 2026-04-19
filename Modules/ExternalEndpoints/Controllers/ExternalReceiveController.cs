using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.ExternalEndpoints.Services;
using System.Text.Json;

namespace MyApi.Modules.ExternalEndpoints.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/external-receive")]
    public class ExternalReceiveController : ControllerBase
    {
        private readonly IExternalEndpointService _service;
        private readonly ILogger<ExternalReceiveController> _logger;

        // Body cap for the public receive endpoint. 1 MiB is plenty for typical
        // webhook payloads and protects us from accidental/malicious huge POSTs.
        private const long MaxBodyBytes = 1 * 1024 * 1024;

        // Headers we never want to persist in plain text — auth material, cookies,
        // and anything that could be replayed if the log table is ever exposed.
        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Proxy-Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Api-Key",
            "X-Auth-Token",
            "X-Access-Token",
            "X-Csrf-Token",
        };

        public ExternalReceiveController(IExternalEndpointService service, ILogger<ExternalReceiveController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("{slug}")]
        [HttpGet("{slug}")]
        [HttpPut("{slug}")]
        [RequestSizeLimit(MaxBodyBytes)]
        public async Task<IActionResult> Receive(string slug)
        {
            try
            {
                var method = Request.Method;
                var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
                var queryString = Request.QueryString.Value;

                // Collect headers, redacting sensitive ones so credentials never
                // end up in the ExternalEndpointLogs table.
                var headers = JsonSerializer.Serialize(
                    Request.Headers.ToDictionary(
                        h => h.Key,
                        h => SensitiveHeaders.Contains(h.Key) ? "[REDACTED]" : h.Value.ToString())
                );

                // Read body with explicit size guard (defense in depth on top of
                // [RequestSizeLimit] — Kestrel may not always enforce it the same
                // way behind reverse proxies).
                string? body = null;
                if (method != "GET")
                {
                    if (Request.ContentLength.HasValue && Request.ContentLength.Value > MaxBodyBytes)
                    {
                        return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "Payload too large" });
                    }
                    using var reader = new StreamReader(Request.Body);
                    body = await reader.ReadToEndAsync();
                    if (body.Length > MaxBodyBytes)
                    {
                        return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "Payload too large" });
                    }
                }

                var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var (statusCode, responseBody) = await _service.ReceiveAsync(slug, method, headers, queryString, body, sourceIp, apiKey);

                // Detect JSON vs plain text so user-defined ResponseTemplate values never crash the endpoint.
                var raw = responseBody ?? string.Empty;
                var trimmed = raw.TrimStart();
                var looksLikeJson = trimmed.StartsWith("{") || trimmed.StartsWith("[") || trimmed.StartsWith("\"");
                return new ContentResult
                {
                    Content = raw,
                    ContentType = looksLikeJson ? "application/json" : "text/plain",
                    StatusCode = statusCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving data for slug {Slug}", slug);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
