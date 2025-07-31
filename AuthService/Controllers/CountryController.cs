using Common;
using DTOs.GeographyDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.GeographyQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CountryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene todos los países con sus estados
    /// </summary>
    [HttpGet("GetAll")]
    public async Task<ActionResult<ApiResponse<List<CountryDTO>>>> GetAll()
    {
        var query = new GetAllCountriesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success.HasValue || !result.Success.Value)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene un país por ID con sus estados
    /// </summary>
    [HttpGet("GetById/{id:int}")]
    public async Task<ActionResult<ApiResponse<CountryDTO>>> GetById(int id)
    {
        var query = new GetCountryByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success.HasValue || !result.Success.Value)
            return NotFound(result);

        return Ok(result);
    }
}
