using AuthService.DTOs.PermissionDTOs;
using Commands.PermissionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.PermissionQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HasPermission("Permission.Create")]
        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] PermissionDTO permissionDto
        )
        {
            var command = new CreatePermissionCommands(permissionDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a permission" });
            return Ok(result);
        }

        [HasPermission("Permission.Update")]
        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(
            [FromBody] PermissionDTO permissionDto
        )
        {
            var command = new UpdatePermissionCommands(permissionDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to update a permission" });
            return Ok(result);
        }

        [HasPermission("Permission.Delete")]
        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeletePermissionCommands(id);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to delete a permission" });
            return Ok(result);
        }

        [HasPermission("Permission.Read")]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var command = new GetAllPermissionQuery();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HasPermission("Permission.Read")]
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var command = new GetPermissionByIdQuery(id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [Authorize]
        [HttpGet("user/{userId:guid}/codes")]
        public async Task<IActionResult> CodesByUser(Guid userId)
        {
            var command = new GetUserPermissionsQuery(userId);
            var result = await _mediator.Send(command);

            if (!(result?.Success ?? false))
                return NotFound(result?.Message ?? "Permissions not found");

            return Ok(result!.Data!.PermissionCodes);
        }
    }
}
