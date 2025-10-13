using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.Shared.DTOs;
using MyApi.Modules.Users.DTOs;
using MyApi.Modules.Users.Services;
using System.Security.Claims;

namespace MyApi.Modules.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Only authenticated MainAdminUsers can access
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(new ApiResponse<UserListResponseDto>
                {
                    Success = true,
                    Message = "Users retrieved successfully",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while retrieving users"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserResponseDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while retrieving user"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Get the current user's email from JWT claims
                var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Unable to identify current user"
                    });
                }

                var newUser = await _userService.CreateUserAsync(createDto, currentUserEmail);
                
                return CreatedAtAction(
                    nameof(GetUser), 
                    new { id = newUser.Id }, 
                    new ApiResponse<UserResponseDto>
                    {
                        Success = true,
                        Message = "User created successfully",
                        Data = newUser
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while creating user"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateRegularUserRequestDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Get the current user's email from JWT claims
                var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Unable to identify current user"
                    });
                }

                var updatedUser = await _userService.UpdateUserAsync(id, updateDto, currentUserEmail);
                if (updatedUser == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserResponseDto>
                {
                    Success = true,
                    Message = "User updated successfully",
                    Data = updatedUser
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while updating user"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // Get the current user's email from JWT claims
                var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Unable to identify current user"
                    });
                }

                var deleted = await _userService.DeleteUserAsync(id, currentUserEmail);
                if (!deleted)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "User deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while deleting user"
                });
            }
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangeUserPassword(int id, [FromBody] ChangeUserPasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Get the current user's email from JWT claims
                var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Unable to identify current user"
                    });
                }

                var success = await _userService.ChangeUserPasswordAsync(id, changePasswordDto.NewPassword, currentUserEmail);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while changing password"
                });
            }
        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserResponseDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while retrieving user"
                });
            }
        }
    }
}
