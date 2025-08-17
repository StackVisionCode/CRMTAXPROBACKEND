using AuthService.DTOs.ModuleDTOs;
using Commands.ModuleCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.ModuleQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Solo usuarios autenticados
public class ModuleController : ControllerBase
{
    private readonly IMediator _mediator;

    public ModuleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Crear un nuevo Module (Solo Developer)
    /// </summary>
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<ModuleDTO>>> CreateModule(
        [FromBody] NewModuleDTO moduleDto
    )
    {
        var command = new CreateModuleCommand(moduleDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Module por ID
    /// </summary>
    [HttpGet("GetById/{moduleId}")]
    public async Task<ActionResult<ApiResponse<ModuleDTO>>> GetModuleById(Guid moduleId)
    {
        var query = new GetModuleByIdQuery(moduleId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los Modules (Solo Developer)
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ModuleDTO>>>> GetAllModules(
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? serviceId = null
    )
    {
        var query = new GetAllModulesQuery(isActive, serviceId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Modules activos
    /// </summary>
    [HttpGet("GetActive")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ModuleDTO>>>> GetActiveModules(
        [FromQuery] Guid? serviceId = null
    )
    {
        var query = new GetActiveModulesQuery(serviceId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Modules disponibles para CustomPlan
    /// </summary>
    [HttpGet("GetAvailableForCustomPlan")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<ModuleDTO>>>
    > GetAvailableModulesForCustomPlan()
    {
        var query = new GetAvailableModulesForCustomPlanQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Modules por Service ID
    /// </summary>
    [HttpGet("GetByService/{serviceId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ModuleDTO>>>> GetModulesByService(
        Guid serviceId
    )
    {
        var query = new GetModulesByServiceQuery(serviceId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener Modules con estadísticas (Solo Developer)
    /// </summary>
    [HttpGet("GetWithStats")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<ModuleWithStatsDTO>>>
    > GetModulesWithStats()
    {
        var query = new GetModulesWithStatsQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar Module (Solo Developer)
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<ModuleDTO>>> UpdateModule(
        [FromBody] UpdateModuleDTO moduleDto
    )
    {
        var command = new UpdateModuleCommand(moduleDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Eliminar Module (Solo Developer)
    /// </summary>
    [HttpDelete("Delete/{moduleId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteModule(Guid moduleId)
    {
        var command = new DeleteModuleCommand(moduleId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Activar/Desactivar Module (Solo Developer)
    /// </summary>
    [HttpPatch("ToggleStatus/{moduleId}")]
    public async Task<ActionResult<ApiResponse<ModuleDTO>>> ToggleModuleStatus(
        Guid moduleId,
        [FromBody] bool isActive
    )
    {
        var command = new ToggleModuleStatusCommand(moduleId, isActive);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Asignar Module a Service (Solo Developer)
    /// </summary>
    [HttpPatch("AssignToService/{moduleId}")]
    public async Task<ActionResult<ApiResponse<ModuleDTO>>> AssignModuleToService(
        Guid moduleId,
        [FromBody] AssignModuleToServiceDTO assignDto
    )
    {
        // Validar que el moduleId del endpoint coincida con el del DTO
        if (moduleId != assignDto.ModuleId)
        {
            return BadRequest(new ApiResponse<ModuleDTO>(false, "Module ID mismatch", null!));
        }

        var command = new AssignModuleToServiceCommand(assignDto.ModuleId, assignDto.ServiceId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
