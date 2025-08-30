using Commands.CustomModuleCommands;
using Common;
using DTOs.CustomModuleDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CustomModuleQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomModuleController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomModuleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Asignar un Module a un CustomPlan (Solo Developer)
    /// </summary>
    [HttpPost("Assign")]
    public async Task<ActionResult<ApiResponse<CustomModuleDTO>>> AssignCustomModule(
        [FromBody] AssignCustomModuleDTO customModuleDto
    )
    {
        var command = new AssignCustomModuleCommand(customModuleDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomModule por ID
    /// </summary>
    [HttpGet("GetById/{customModuleId}")]
    public async Task<ActionResult<ApiResponse<CustomModuleDTO>>> GetCustomModuleById(
        Guid customModuleId
    )
    {
        var query = new GetCustomModuleByIdQuery(customModuleId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los CustomModules (Solo Developer)
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomModuleDTO>>>> GetAllCustomModules(
        [FromQuery] bool? isIncluded = null,
        [FromQuery] Guid? customPlanId = null
    )
    {
        var query = new GetAllCustomModulesQuery(isIncluded, customPlanId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomModules por CustomPlan ID
    /// </summary>
    [HttpGet("GetByPlan/{customPlanId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomModuleDTO>>>
    > GetCustomModulesByPlan(Guid customPlanId, [FromQuery] bool? isIncluded = null)
    {
        var query = new GetCustomModulesByPlanQuery(customPlanId, isIncluded);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomModules por Module ID
    /// </summary>
    [HttpGet("GetByModule/{moduleId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomModuleDTO>>>
    > GetCustomModulesByModule(Guid moduleId, [FromQuery] bool? isIncluded = null)
    {
        var query = new GetCustomModulesByModuleQuery(moduleId, isIncluded);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomModules activos por Company ID
    /// </summary>
    [HttpGet("GetActiveByCompany/{companyId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomModuleDTO>>>
    > GetActiveCustomModulesByCompany(Guid companyId)
    {
        var query = new GetActiveCustomModulesByCompanyQuery(companyId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomModules con estadísticas (Solo Developer)
    /// </summary>
    [HttpGet("GetWithStats")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>>
    > GetCustomModulesWithStats()
    {
        var query = new GetCustomModulesWithStatsQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener módulos disponibles para un CustomPlan
    /// </summary>
    [HttpGet("GetAvailableForPlan/{customPlanId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<ModuleAvailabilityDTO>>>
    > GetAvailableModulesForCustomPlan(Guid customPlanId)
    {
        var query = new GetAvailableModulesForCustomPlanQuery(customPlanId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar CustomModule (Solo Developer)
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<CustomModuleDTO>>> UpdateCustomModule(
        [FromBody] UpdateCustomModuleDTO customModuleDto
    )
    {
        var command = new UpdateCustomModuleCommand(customModuleDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Remover CustomModule (Solo Developer)
    /// </summary>
    [HttpDelete("Remove/{customModuleId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveCustomModule(Guid customModuleId)
    {
        var command = new RemoveCustomModuleCommand(customModuleId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Activar/Desactivar CustomModule (Solo Developer)
    /// </summary>
    [HttpPatch("Toggle/{customModuleId}")]
    public async Task<ActionResult<ApiResponse<CustomModuleDTO>>> ToggleCustomModule(
        Guid customModuleId,
        [FromBody] bool isIncluded
    )
    {
        var command = new ToggleCustomModuleCommand(customModuleId, isIncluded);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Asignar múltiples módulos a un CustomPlan (Solo Developer)
    /// </summary>
    [HttpPost("BulkAssign/{customPlanId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomModuleDTO>>>
    > BulkAssignCustomModules(Guid customPlanId, [FromBody] ICollection<Guid> moduleIds)
    {
        var command = new BulkAssignCustomModulesCommand(customPlanId, moduleIds);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Remover múltiples módulos de un CustomPlan (Solo Developer)
    /// </summary>
    [HttpDelete("BulkRemove/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkRemoveCustomModules(
        Guid customPlanId,
        [FromBody] ICollection<Guid> moduleIds
    )
    {
        var command = new BulkRemoveCustomModulesCommand(customPlanId, moduleIds);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
