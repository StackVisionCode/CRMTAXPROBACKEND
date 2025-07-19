using Application.Dtos;
using Application.Dtos.Product;
using AutoMapper;
using Infrastructure.Command.Product;
using Infrastructure.Querys.Templates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Controllers.Product;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    // POST: api/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var result = await _mediator.Send(new CreateProductCommand(dto));
        return Ok(result);

    }

     [HttpPost("like")]
    public async Task<IActionResult> Like([FromBody] LikeProductDto dto)
        => Ok(await _mediator.Send(new LikeProductCommand(dto)));

    [HttpPost("dislike")]
    public async Task<IActionResult> Dislike([FromBody] DislikeProductDto dto)
        => Ok(await _mediator.Send(new DislikeProductCommand(dto)));

    [HttpPost("rate")]
    public async Task<IActionResult> Rate([FromBody] RateDto dto)
        => Ok(await _mediator.Send(new RateProductCommand(dto)));

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllProductsQuery());
        return Ok(result);
    }
}
