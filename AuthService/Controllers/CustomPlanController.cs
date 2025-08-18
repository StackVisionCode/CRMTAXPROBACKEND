using AuthService.DTOs.CustomPlanDTOs;
using Commands.CustomPlanCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CustomPlanQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomPlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomPlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Crear un nuevo CustomPlan (Solo Developer)
    /// </summary>
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> CreateCustomPlan(
        [FromBody] NewCustomPlanDTO customPlanDto
    )
    {
        var command = new CreateCustomPlanCommand(customPlanDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomPlan por ID
    /// </summary>
    [HttpGet("GetById/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> GetCustomPlanById(Guid customPlanId)
    {
        var query = new GetCustomPlanByIdQuery(customPlanId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los CustomPlans (Solo Developer)
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomPlanDTO>>>> GetAllCustomPlans(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isExpired = null
    )
    {
        var query = new GetAllCustomPlansQuery(isActive, isExpired);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomPlan por Company ID
    /// </summary>
    [HttpGet("GetByCompany/{companyId}")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> GetCustomPlanByCompany(
        Guid companyId
    )
    {
        var query = new GetCustomPlanByCompanyQuery(companyId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomPlans que expiran pronto (Solo Developer)
    /// </summary>
    [HttpGet("GetExpiring")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomPlanDTO>>>> GetExpiringCustomPlans(
        [FromQuery] int daysAhead = 30
    )
    {
        var query = new GetExpiringCustomPlansQuery(daysAhead);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomPlans con estadísticas (Solo Developer)
    /// </summary>
    [HttpGet("GetWithStats")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>>
    > GetCustomPlansWithStats()
    {
        var query = new GetCustomPlansWithStatsQuery();
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CustomPlans por rango de precios (Solo Developer)
    /// </summary>
    [HttpGet("GetByPriceRange")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CustomPlanDTO>>>
    > GetCustomPlansByPriceRange([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
    {
        var query = new GetCustomPlansByPriceRangeQuery(minPrice, maxPrice);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar CustomPlan (Solo Developer)
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> UpdateCustomPlan(
        [FromBody] UpdateCustomPlanDTO customPlanDto
    )
    {
        var command = new UpdateCustomPlanCommand(customPlanDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Eliminar CustomPlan (Solo Developer)
    /// </summary>
    [HttpDelete("Delete/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCustomPlan(Guid customPlanId)
    {
        var command = new DeleteCustomPlanCommand(customPlanId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Activar/Desactivar CustomPlan (Solo Developer)
    /// </summary>
    [HttpPatch("ToggleStatus/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> ToggleCustomPlanStatus(
        Guid customPlanId,
        [FromBody] bool isActive
    )
    {
        var command = new ToggleCustomPlanStatusCommand(customPlanId, isActive);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Renovar CustomPlan (Solo Developer)
    /// </summary>
    [HttpPatch("Renew/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> RenewCustomPlan(
        Guid customPlanId,
        [FromBody] DateTime? newEndDate = null
    )
    {
        var command = new RenewCustomPlanCommand(customPlanId, newEndDate);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar precio de CustomPlan (Solo Developer)
    /// </summary>
    [HttpPatch("UpdatePrice/{customPlanId}")]
    public async Task<ActionResult<ApiResponse<CustomPlanDTO>>> UpdateCustomPlanPrice(
        Guid customPlanId,
        [FromBody] decimal newPrice
    )
    {
        var command = new UpdateCustomPlanPriceCommand(customPlanId, newPrice);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }
}
