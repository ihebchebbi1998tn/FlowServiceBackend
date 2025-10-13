using MyApi.Modules.Auth.DTOs;
using MyApi.Modules.Auth.Services;
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

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user login
        /// </summary>
        /// <param name="loginRequest">Login credentials</param>
        /// <returns>Authentication response with user data and tokens</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
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

                var response = await _authService.LoginAsync(loginRequest);

                if (!response.Success)
                {
                    return Unauthorized(response);
                }

                _logger.LogInformation("User logged in successfully: {Email}", loginRequest.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
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
                    return Unauthorized(response);
                }

                _logger.LogInformation("Regular user logged in successfully: {Email}", loginRequest.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
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
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var success = await _authService.LogoutAsync(userId);

                if (!success)
                {
                    return StatusCode(500, new { message = "An error occurred during logout" });
                }

                _logger.LogInformation("User logged out successfully: {UserId}", userId);
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
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
    }
}
