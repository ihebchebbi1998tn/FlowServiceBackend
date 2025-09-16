using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(roles, "Roles retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve roles"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Role not found"));
                }

                return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role {RoleId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve role"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Role name is required"));
                }

                if (await _roleService.RoleExistsAsync(request.Name))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("A role with this name already exists"));
                }

                var createdBy = "System"; // TODO: Get from authenticated user
                var role = await _roleService.CreateRoleAsync(request, createdBy);

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, 
                    ApiResponse<RoleDto>.SuccessResponse(role, "Role created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to create role"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Role name is required"));
                }

                if (await _roleService.RoleExistsAsync(request.Name, id))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("A role with this name already exists"));
                }

                var modifiedBy = "System"; // TODO: Get from authenticated user
                var role = await _roleService.UpdateRoleAsync(id, request, modifiedBy);

                return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role updated successfully"));
            }
            catch (ArgumentException)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Role not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to update role"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRole(int id)
        {
            try
            {
                var success = await _roleService.DeleteRoleAsync(id);
                if (!success)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Role not found"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "Role deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete role"));
            }
        }

        [HttpPost("{roleId}/assign/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRoleToUser(int roleId, int userId)
        {
            try
            {
                var assignedBy = "System"; // TODO: Get from authenticated user
                var success = await _roleService.AssignRoleToUserAsync(userId, roleId, assignedBy);
                
                return Ok(ApiResponse<object>.SuccessResponse(null, "Role assigned to user successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to assign role to user"));
            }
        }

        [HttpDelete("{roleId}/remove/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveRoleFromUser(int roleId, int userId)
        {
            try
            {
                var success = await _roleService.RemoveRoleFromUserAsync(userId, roleId);
                if (!success)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Role assignment not found"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "Role removed from user successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to remove role from user"));
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetUserRoles(int userId)
        {
            try
            {
                var roles = await _roleService.GetUserRolesAsync(userId);
                return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(roles, "User roles retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve user roles"));
            }
        }
    }
}