// using AuthService.DTOs.RoleDTOs;
// using AuthService.Services.RolePermissionServices;
// using Common;
// using Microsoft.AspNetCore.Mvc;

// namespace AuthService.Controllers
// {
//   [ApiController]
//   [Route("api/[controller]")]
//   public class RolePermissionController : ControllerBase
//   {
//     private readonly IReadCommandRolePermissions _readRolePermission;

//     public RolePermissionController(IReadCommandRolePermissions readRolePermission)
//     {
//       _readRolePermission = readRolePermission;
//     }

//     [HttpGet("GetAllRolePermissions")]
//     public async Task<IActionResult> GetRolePermissions()
//     {
//       ApiResponse<List<RolePermissionDTO>> result = await _readRolePermission.GetAll();
//       if (result.Success == false) return BadRequest(new { result });

//       return Ok(result);
//     }

//     [HttpGet("GetRolePermissionById")]
//     public async Task<IActionResult> GetRolePermissionById(int rolePermissionId)
//     {
//       ApiResponse<RolePermissionDTO> result = await _readRolePermission.GetById(rolePermissionId);
//       if (result.Success == false)
//       {
//         return BadRequest(new { result });
//       }

//       return Ok(result);
//     }

//     [HttpGet("GetRolesAndPermissions")]
//     public async Task<IActionResult> GetRolesAndPermissions(int userId)
//     {
//       var result = await _readRolePermission.GetRolesAndPermissionsByUserId(userId);

//       if (!result.Success)
//         return NotFound(result);

//       return Ok(result);
//     }
//   }
// }