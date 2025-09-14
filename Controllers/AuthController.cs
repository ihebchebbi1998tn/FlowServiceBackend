using FlowServiceBackend.DTOs;
using FlowServiceBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlowServiceBackend.Controllers
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
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid signup data provided"
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
    }
}