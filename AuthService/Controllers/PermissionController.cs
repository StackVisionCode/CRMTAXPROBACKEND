// using AuthService.DTOs.PermissionDTOs;
// using AuthService.Services.PermissionServices;
// using Common;
// using Microsoft.AspNetCore.Mvc;

// namespace AuthService.Controllers
// {
//   [ApiController]
//   [Route("api/[controller]")]
//   public class PermissionController : ControllerBase
//   {
//     private readonly IReadCommandPermissions _readPermission;

//     public PermissionController(IReadCommandPermissions readPermission)
//     {
//       _readPermission = readPermission;
//     }

//     [HttpGet("GetAllPermissions")]
//     public async Task<IActionResult> GetPermissions()
//     {
//       ApiResponse<List<PermissionDTO>> result = await _readPermission.GetAll();
//       if (result.Success == false) return BadRequest(new { result });

//       return Ok(result);
//     }

//     [HttpGet("GetPermissionById")]
//     public async Task<IActionResult> GetPermissionById(int permissionId)
//     {
//       ApiResponse<PermissionDTO> result = await _readPermission.GetById(permissionId);
//       if (result.Success == false)
//       {
//         return BadRequest(new { result });
//       }

//       return Ok(result);
//     }
//   }
// }