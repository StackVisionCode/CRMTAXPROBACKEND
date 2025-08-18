using AuthService.DTOs.ServiceDTOs;
using Commands.ServiceCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.ServiceQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Solo usuarios autenticados
public class ServiceController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Crear un nuevo Service (Solo Developer)
    /// </summary>
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<ServiceDTO>>> CreateService(
        [FromBody] NewServiceDTO serviceDto
    )
    {
        var command = new CreateServiceCommand(serviceDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Service por ID
    /// </summary>
    [HttpGet("GetById/{serviceId}")]
    public async Task<ActionResult<ApiResponse<ServiceDTO>>> GetServiceById(Guid serviceId)
    {
        var query = new GetServiceByIdQuery(serviceId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los Services (Solo Developer)
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDTO>>>> GetAllServices(
        [FromQuery] bool? isActive = null
    )
    {
        var query = new GetAllServicesQuery(isActive);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Services activos para selección
    /// </summary>
    [HttpGet("GetActive")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ServiceDTO>>>> GetActiveServices()
    {
        var query = new GetActiveServicesQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Services con estadísticas (Solo Developer)
    /// </summary>
    [HttpGet("GetWithStats")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<ServiceWithStatsDTO>>>
    > GetServicesWithStats()
    {
        var query = new GetServicesWithStatsQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar Service (Solo Developer)
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<ServiceDTO>>> UpdateService(
        [FromBody] UpdateServiceDTO serviceDto
    )
    {
        var command = new UpdateServiceCommand(serviceDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Eliminar Service (Solo Developer)
    /// </summary>
    [HttpDelete("Delete/{serviceId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteService(Guid serviceId)
    {
        var command = new DeleteServiceCommand(serviceId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Activar/Desactivar Service (Solo Developer)
    /// </summary>
    [HttpPatch("ToggleStatus/{serviceId}")]
    public async Task<ActionResult<ApiResponse<ServiceDTO>>> ToggleServiceStatus(
        Guid serviceId,
        [FromBody] bool isActive
    )
    {
        var command = new ToggleServiceStatusCommand(serviceId, isActive);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
