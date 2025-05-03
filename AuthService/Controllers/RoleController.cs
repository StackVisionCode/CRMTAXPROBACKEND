// using AuthService.DTOs.RoleDTOs;
// using AuthService.Services.RoleServices;
// using Common;
// using Microsoft.AspNetCore.Mvc;

// namespace AuthService.Controllers;
// [ApiController]
// [Route("api/[controller]")]
// public class RoleController : ControllerBase
// {
//   private readonly IReadCommandRoles _readRole;

//   public RoleController(IReadCommandRoles readRole)
//   {
//     _readRole = readRole;
//   }

//   [HttpGet("GetAllRolesAll")]
//   public async Task<IActionResult> GetAllRolesAll()
//   {
//     var result = await _readRole.GetAll();
//     if (result.Success == false) return BadRequest(new { result });

//         return Ok(result);
//   }

//   [HttpGet("GetByRoleId")]
//   public async Task<IActionResult> GetRoleById(int RoleId)
//   {
//     ApiResponse<RoleDTO> result = await _readRole.GetbyId(RoleId);
//     if (result.Success == false)
//     {
//       return BadRequest(new { result });
//     }

//     return Ok(result);
//   }

//   [HttpGet("GetRoleByUserId")]
//   public async Task<IActionResult> GetRoleByUserId(int userId)
//   {
//     var result = await _readRole.GetByUserId(userId);

//     if (!result.Success)
//     {
//       return NotFound(result);
//     }

//     return Ok(result);
//   }
// }