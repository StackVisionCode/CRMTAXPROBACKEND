// using AuthService.DTOs.SessionDTOs;
// using AuthService.Services.SessionServices;
// using Common;
// using Microsoft.AspNetCore.Mvc;

// namespace AuthService.Controllers
// {
//   [ApiController]
//   [Route("api/[controller]")]
//   public class SessionController : ControllerBase
//   {
//     private readonly ISessionCommandRead _sessionRead;

//     public SessionController(ISessionCommandRead sessionRead)
//     {
//       _sessionRead = sessionRead;
//     }

//     [HttpGet("GetAllSessions")]
//     public async Task<IActionResult> GetAllSessions()
//     {
//       ApiResponse<List<SessionDTO>> result = await _sessionRead.GetAll();

//       if (result.Success == false)
//       {
//         return BadRequest(new { result });
//       }

//       return Ok(result);
//     }

//     [HttpGet("GetSessionById")]
//     public async Task<IActionResult> GetSessionById(int sessionId)
//     {
//       ApiResponse<SessionDTO> result = await _sessionRead.GetById(sessionId);

//       if (result.Success == false)
//       {
//         return BadRequest(new { result });
//       }

//       return Ok(result);
//     }

//     [HttpGet("GetSessionByUserId")]
//     public async Task<IActionResult> GetSessionByUserId(int userId)
//     {
//       ApiResponse<List<SessionDTO>> result = await _sessionRead.GetByUserId(userId);

//       if (result.Success == false)
//       {
//         return BadRequest(new { result });
//       }

//       return Ok(result);
//     }
//   }
// }