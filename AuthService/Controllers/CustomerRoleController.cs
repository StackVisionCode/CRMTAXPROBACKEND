using AuthService.DTOs.RoleDTOs;
using Commands.CustomerRoleCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CustomerRoleQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerRoleController : ControllerBase
{
    private readonly IMediator _med;

    public CustomerRoleController(IMediator med)
    {
        _med = med;
    }

    [HasPermission("RolePermission.Create")]
    [HttpPost("Assign")]
    public async Task<ActionResult<ApiResponse<bool>>> Assign(Guid customerId, Guid roleId)
    {
        var command = new AssignRoleToCustomerCommand(customerId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    [HasPermission("RolePermission.Delete")]
    [HttpDelete("Remove")]
    public async Task<ActionResult<ApiResponse<bool>>> Remove(Guid customerId, Guid roleId)
    {
        var command = new RemoveRoleFromCustomerCommand(customerId, roleId);
        var result = await _med.Send(command);
        return Ok(result);
    }

    [HasPermission("Role.Read")]
    [HttpGet("{customerId:guid}/roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetRolesByCustomer(Guid customerId)
    {
        var command = new GetRolesByCustomerIdQuery(customerId);
        var result = await _med.Send(command);
        return Ok(result);
    }
}
