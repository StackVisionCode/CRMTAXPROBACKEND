using AuthService.DTOs.RoleDTOs;
using Commands.RoleCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.RoleQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IMediator mediator, ILogger<RoleController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Crear un nuevo rol
    /// </summary>
    [HasPermission("Role.Create")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] RoleDTO roleDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for role creation: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        var command = new CreateRoleCommands(roleDto);
        var result = await _mediator.Send(command);

        return result.Success ?? false ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Actualizar un rol existente
    /// </summary>
    [HasPermission("Role.Update")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] RoleDTO roleDto)
    {
        if (id != roleDto.Id)
        {
            _logger.LogWarning(
                "Route ID {RouteId} does not match body ID {BodyId}",
                id,
                roleDto.Id
            );
            return BadRequest("Route ID must match role ID in body");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for role update: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        var command = new UpdateRoleCommands(roleDto);
        var result = await _mediator.Send(command);

        return result.Success ?? false ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Eliminar un rol
    /// </summary>
    [HasPermission("Role.Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var command = new DeleteRoleCommands(id);
        var result = await _mediator.Send(command);

        return result.Success ?? false ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Obtener todos los roles
    /// </summary>
    // [HasPermission("Role.Read")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetAll()
    {
        var query = new GetAllRoleQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener un rol por ID
    /// </summary>
    // [HasPermission("Role.Read")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDTO>>> GetById(Guid id)
    {
        var query = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(query);

        return result.Success ?? false ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Obtener roles filtrados por ServiceLevel
    /// </summary>
    // [HasPermission("Role.Read")]
    [HttpGet("by-service-level/{serviceLevel}")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetByServiceLevel(int serviceLevel)
    {
        var query = new GetAllRoleQuery();
        var result = await _mediator.Send(query);

        if (result.Success ?? false && result.Data != null)
        {
            var filteredRoles = result
                .Data!.Where(r => r.ServiceLevel != null && (int)r.ServiceLevel == serviceLevel)
                .ToList();

            return Ok(
                new ApiResponse<List<RoleDTO>>(
                    true,
                    $"Roles for service level {serviceLevel} retrieved successfully",
                    filteredRoles
                )
            );
        }

        return BadRequest(result);
    }

    /// <summary>
    ///  Obtener roles disponibles para Owners vs Users regulares
    /// </summary>
    // [HasPermission("Role.Read")]
    [HttpGet("available-for/{userType}")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetAvailableRoles(string userType)
    {
        var query = new GetAllRoleQuery();
        var result = await _mediator.Send(query);

        if (result.Success ?? false && result.Data != null)
        {
            List<RoleDTO> filteredRoles;

            switch (userType.ToLower())
            {
                case "owner":
                    filteredRoles = result
                        .Data!.Where(r => r.Name == "Developer" || r.Name.Contains("Administrator"))
                        .ToList();
                    break;
                case "user":
                    filteredRoles = result.Data!.Where(r => r.Name == "User").ToList();
                    break;
                case "customer":
                    filteredRoles = result.Data!.Where(r => r.Name == "Customer").ToList();
                    break;
                default:
                    return BadRequest(
                        $"Invalid user type: {userType}. Valid types: owner, user, customer"
                    );
            }

            return Ok(
                new ApiResponse<List<RoleDTO>>(
                    true,
                    $"Available roles for {userType} retrieved successfully",
                    filteredRoles
                )
            );
        }

        return BadRequest(result);
    }
}
