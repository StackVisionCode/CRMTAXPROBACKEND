using Common;
using DTOs.GeographyDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.GeographyQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StateController : ControllerBase
{
    private readonly IMediator _mediator;

    public StateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene todos los estados con información del país
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<List<StateDTO>>>> GetAll()
    {
        var query = new GetAllStatesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success.HasValue || !result.Success.Value)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un estado por ID con información del país
    /// </summary>
    [HttpGet("GetById/{id:int}")]
    public async Task<ActionResult<ApiResponse<StateDTO>>> GetById(int id)
    {
        var query = new GetStateByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success.HasValue || !result.Success.Value)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene todos los estados de un país específico
    /// </summary>
    [HttpGet("GetByCountry/{countryId:int}")]
    public async Task<ActionResult<ApiResponse<List<StateDTO>>>> GetByCountryId(int countryId)
    {
        var query = new GetStatesByCountryIdQuery(countryId);
        var result = await _mediator.Send(query);

        if (!result.Success.HasValue || !result.Success.Value)
            return BadRequest(result);

        return Ok(result);
    }
}
