using MyApi.Modules.Auth.DTOs;
using MyApi.Modules.Auth.Services;
using MyApi.Modules.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyApi.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly ISystemLogService _systemLogService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, ISystemLogService systemLogService)
        {
            _authService = authService;
            _logger = logger;
            _systemLogService = systemLogService;
        }

        /// <summary>
        /// Check if an admin user already exists in the system
        /// Also returns admin preferences for theming the login page
        /// </summary>
        /// <returns>Admin exists status and preferences for login page theming</returns>
        [HttpGet("admin-exists")]
        public async Task<ActionResult<AdminExistsResultDto>> CheckAdminExists()
        {
            try
            {
                var result = await _authService.GetAdminExistsWithPreferencesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if admin exists");
                return StatusCode(500, new AdminExistsResultDto 
                { 
                    AdminExists = false, 
                    SignupAllowed = true, 
                    Message = "Error checking admin status" 
                });
            }
        }

        /// <summary>
        /// Authenticate user login
        /// </summary>
        /// <param name="loginRequest">Login credentials</param>
        /// <returns>Authentication response with user data and tokens</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid login data provided"
                    });
                }

                var response = await _authService.LoginAsync(loginRequest);

                if (!response.Success)
                {
                    // Log failed login attempt
                    await _systemLogService.LogWarningAsync(
                        $"Failed login attempt for email: {loginRequest.Email}",
                        "Auth",
                        "login",
                        details: response.Message
                    );
                    return Unauthorized(response);
                }

                // Log successful login
                await _systemLogService.LogSuccessAsync(
                    $"Admin user logged in successfully: {loginRequest.Email}",
                    "Auth",
                    "login",
                    userId: response.User?.Id.ToString(),
                    userName: $"{response.User?.FirstName} {response.User?.LastName}"
                );

                _logger.LogInformation("User logged in successfully: {Email}", loginRequest.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                await _systemLogService.LogErrorAsync(
                    $"Error during login attempt for email: {loginRequest.Email}",
                    "Auth",
                    "login",
                    details: ex.Message
                );
                _logger.LogError(ex, "Error during login attempt for email: {Email}", loginRequest.Email);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during login"
                });
            }
        }

        /// <summary>
        /// Authenticate regular user login (Users table)
        /// </summary>
        /// <param name="loginRequest">Login credentials</param>
        /// <returns>Authentication response with user data and tokens</returns>
        [HttpPost("user-login")]
        public async Task<ActionResult<AuthResponseDto>> UserLogin([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid login data provided"
                    });
                }

                var response = await _authService.UserLoginAsync(loginRequest);

                if (!response.Success)
                {
                    // Log failed login attempt
                    await _systemLogService.LogWarningAsync(
                        $"Failed user login attempt for email: {loginRequest.Email}",
                        "Auth",
                        "login",
                        details: response.Message
                    );
                    return Unauthorized(response);
                }

                // Log successful login
                await _systemLogService.LogSuccessAsync(
                    $"User logged in successfully: {loginRequest.Email}",
                    "Auth",
                    "login",
                    userId: response.User?.Id.ToString(),
                    userName: $"{response.User?.FirstName} {response.User?.LastName}"
                );

                _logger.LogInformation("Regular user logged in successfully: {Email}", loginRequest.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                await _systemLogService.LogErrorAsync(
                    $"Error during user login attempt for email: {loginRequest.Email}",
                    "Auth",
                    "login",
                    details: ex.Message
                );
                _logger.LogError(ex, "Error during user login attempt for email: {Email}", loginRequest.Email);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during login"
                });
            }
        }

        /// <summary>
        /// Register new user account
        /// </summary>
        /// <param name="signupRequest">User registration data</param>
        /// <returns>Authentication response with user data and tokens</returns>
        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] SignupRequestDto signupRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) ?? Array.Empty<string>() })
                        .ToArray();
                    
                    _logger.LogWarning("Invalid signup data provided. Validation errors: {@Errors}", errors);
                    
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Invalid signup data provided: {string.Join(", ", errors.SelectMany(e => e.Errors))}"
                    });
                }

                var response = await _authService.SignupAsync(signupRequest);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("User registered successfully: {Email}", signupRequest.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup attempt for email: {Email}", signupRequest.Email);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during account creation"
                });
            }
        }

        /// <summary>
        /// OAuth login - check if user exists by email
        /// </summary>
        /// <param name="request">OAuth login request with email</param>
        /// <returns>Authentication response or indication to complete signup</returns>
        [HttpPost("oauth-login")]
        public async Task<ActionResult<AuthResponseDto>> OAuthLogin([FromBody] OAuthLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email))
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email is required"
                    });
                }

                var response = await _authService.OAuthLoginAsync(request.Email);

                if (!response.Success)
                {
                    // User not found, need to complete signup
                    return Ok(response);
                }

                _logger.LogInformation("User OAuth login successful: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth login attempt for email: {Email}", request.Email);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during OAuth login"
                });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="refreshRequest">Refresh token request</param>
        /// <returns>New authentication tokens</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto refreshRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid refresh token data"
                    });
                }

                var response = await _authService.RefreshTokenAsync(refreshRequest.RefreshToken);

                if (!response.Success)
                {
                    return Unauthorized(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during token refresh"
                });
            }
        }

        /// <summary>
        /// Get user information by user ID (no authorization required)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User data</returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(string userId)
        {
            try
            {
                if (!int.TryParse(userId, out var userIdInt))
                {
                    return BadRequest(new { message = "Invalid user ID format" });
                }

                var user = await _authService.GetUserByIdAsync(userIdInt);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        /// <summary>
        /// Get all admin users (MainAdminUsers table)
        /// </summary>
        /// <returns>List of admin users</returns>
        [HttpGet("admin-users")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllAdminUsers()
        {
            try
            {
                var users = await _authService.GetAllAdminUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all admin users");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }


        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>Current user data</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        /// <summary>
        /// Update user information by user ID (no authorization required)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="updateRequest">User update data</param>
        /// <returns>Updated user data</returns>
        [HttpPut("user/{userId}")]
        public async Task<ActionResult<AuthResponseDto>> UpdateUser(string userId, [FromBody] UpdateUserRequestDto updateRequest)
        {
            try
            {
                if (!int.TryParse(userId, out var userIdInt))
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid user ID format"
                    });
                }

                var response = await _authService.UpdateUserAsync(userIdInt, updateRequest);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("User updated successfully: {UserId}", userIdInt);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during user update"
                });
            }
        }

        /// <summary>
        /// Update current user information
        /// </summary>
        /// <param name="updateRequest">User update data</param>
        /// <returns>Updated user data</returns>
        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<AuthResponseDto>> UpdateCurrentUser([FromBody] UpdateUserRequestDto updateRequest)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid user token"
                    });
                }

                var response = await _authService.UpdateUserAsync(userId, updateRequest);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("User updated successfully: {UserId}", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during user update"
                });
            }
        }

        /// <summary>
        /// Get the company logo for the login page (no authentication required).
        /// Returns the logo document as a file stream, or the CompanyLogoUrl as JSON.
        /// </summary>
        [HttpGet("company-logo")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCompanyLogo()
        {
            try
            {
                var logoUrl = await _authService.GetCompanyLogoUrlAsync();
                if (string.IsNullOrEmpty(logoUrl))
                {
                    return NotFound(new { message = "No company logo configured" });
                }

                // If it's a doc:{id} reference, try to serve the file directly
                var docMatch = System.Text.RegularExpressions.Regex.Match(logoUrl, @"^doc:(\d+)$");
                if (docMatch.Success && int.TryParse(docMatch.Groups[1].Value, out var docId))
                {
                    // Resolve and stream the document file
                    var result = await _authService.GetCompanyLogoFileAsync(docId);
                    if (result != null)
                    {
                        return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
                    }
                    return NotFound(new { message = "Logo file not found" });
                }

                // Otherwise return the URL as JSON
                return Ok(new { logoUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company logo");
                return StatusCode(500, new { message = "Error retrieving company logo" });
            }
        }

        /// <summary>
        /// Get the company logo as a base64 data URI string — specifically for PDF reports.
        /// This avoids CORS issues that occur when @react-pdf/renderer tries to fetch() static files.
        /// The regular company-logo endpoint and CompanyLogoUrl remain unchanged for normal image display.
        /// </summary>
        [HttpGet("company-logo-base64")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCompanyLogoBase64()
        {
            try
            {
                var logoUrl = await _authService.GetCompanyLogoUrlAsync();
                if (string.IsNullOrEmpty(logoUrl))
                {
                    return Ok(new { logoBase64 = "" });
                }

                // If it's a doc:{id} reference, read the file
                byte[] fileBytes = null;
                string contentType = "image/png";

                var docMatch = System.Text.RegularExpressions.Regex.Match(logoUrl, @"^doc:(\d+)$");
                if (docMatch.Success && int.TryParse(docMatch.Groups[1].Value, out var docId))
                {
                    var result = await _authService.GetCompanyLogoFileAsync(docId);
                    if (result != null)
                    {
                        using var ms = new System.IO.MemoryStream();
                        await result.Value.Stream.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                        contentType = result.Value.ContentType ?? "image/png";
                    }
                }
                else
                {
                    // It's a relative path like "uploads/company/xxx.png" — read directly from disk
                    var relative = logoUrl.TrimStart('/');
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relative);
                    if (System.IO.File.Exists(fullPath))
                    {
                        fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                        // Detect content type from extension
                        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
                        contentType = ext switch
                        {
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".png" => "image/png",
                            ".gif" => "image/gif",
                            ".svg" => "image/svg+xml",
                            ".webp" => "image/webp",
                            ".bmp" => "image/bmp",
                            _ => "image/png"
                        };
                    }
                }

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    return Ok(new { logoBase64 = "" });
                }

                var base64 = $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}";
                return Ok(new { logoBase64 = base64 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company logo as base64");
                return Ok(new { logoBase64 = "" });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="changePasswordRequest">Password change data</param>
        /// <returns>Password change confirmation</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<AuthResponseDto>> ChangePassword([FromBody] ChangePasswordRequestDto changePasswordRequest)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid user token"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid password data provided"
                    });
                }

                var response = await _authService.ChangePasswordAsync(userId, changePasswordRequest);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("User password changed successfully: {UserId}", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An internal error occurred during password change"
                });
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var success = await _authService.LogoutAsync(userId);

                if (!success)
                {
                    await _systemLogService.LogErrorAsync(
                        "Logout failed",
                        "Auth",
                        "logout",
                        userId: userId.ToString()
                    );
                    return StatusCode(500, new { message = "An error occurred during logout" });
                }

                // Log successful logout
                await _systemLogService.LogInfoAsync(
                    $"User logged out: {userNameClaim ?? userId.ToString()}",
                    "Auth",
                    "logout",
                    userId: userId.ToString(),
                    userName: userNameClaim
                );

                _logger.LogInformation("User logged out successfully: {UserId}", userId);
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                await _systemLogService.LogErrorAsync(
                    $"Error during logout: {ex.Message}",
                    "Auth",
                    "logout"
                );
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An internal error occurred during logout" });
            }
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        /// <returns>Authentication status</returns>
        [HttpGet("status")]
        [Authorize]
        public IActionResult GetAuthStatus()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                return Ok(new
                {
                    isAuthenticated = true,
                    userId = userIdClaim,
                    email = emailClaim,
                    name = nameClaim
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking auth status");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        /// <summary>
        /// Test database connection and basic functionality
        /// </summary>
        /// <returns>Database test results</returns>
        [HttpGet("test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var userCount = await _authService.GetUserByEmailAsync("test@example.com");
                var dbConnected = true;
                
                return Ok(new
                {
                    databaseConnected = dbConnected,
                    message = "Database connection test successful",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                return StatusCode(500, new 
                { 
                    databaseConnected = false,
                    message = "Database connection test failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Diagnostic signup test - returns actual error details
        /// </summary>
        /// <param name="signupRequest">User registration data</param>
        /// <returns>Detailed diagnostic info including any errors</returns>
        [HttpPost("test-signup")]
        public async Task<IActionResult> TestSignup([FromBody] SignupRequestDto? signupRequest)
        {
            var diagnostics = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "step", "started" },
                { "email", signupRequest?.Email ?? "null" }
            };

            try
            {
                // Step 0: Check for null request
                if (signupRequest == null)
                {
                    diagnostics["step"] = "null_request";
                    diagnostics["error"] = "Request body is null";
                    return BadRequest(diagnostics);
                }

                // Step 1: Validate model
                diagnostics["step"] = "model_validation";
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        );
                    diagnostics["validation_errors"] = errors;
                    return BadRequest(diagnostics);
                }

                diagnostics["step"] = "calling_service";
                
                // Step 2: Call the actual signup service
                var response = await _authService.SignupAsync(signupRequest);

                diagnostics["step"] = "service_completed";
                diagnostics["success"] = response.Success;
                diagnostics["message"] = response.Message;

                if (!response.Success)
                {
                    diagnostics["step"] = "service_returned_failure";
                    return BadRequest(diagnostics);
                }

                diagnostics["step"] = "completed_successfully";
                diagnostics["userId"] = response.User?.Id ?? 0;
                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics["step"] = "exception_thrown";
                diagnostics["exception_type"] = ex.GetType().FullName ?? "Unknown";
                diagnostics["exception_message"] = ex.Message;
                diagnostics["stack_trace"] = ex.StackTrace?.Split('\n').Take(10).ToArray() ?? Array.Empty<string>();
                if (ex.InnerException != null)
                {
                    diagnostics["inner_exception_type"] = ex.InnerException.GetType().FullName ?? "Unknown";
                    diagnostics["inner_exception_message"] = ex.InnerException.Message;
                }
                
                _logger.LogError(ex, "Test signup diagnostic failed at step: {Step}", diagnostics["step"]);
                return StatusCode(500, diagnostics);
            }
        }
    }
}
