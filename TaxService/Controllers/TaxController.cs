// // TaxController.cs
// using Application.DTOS;
// using Infrastructure.Commands;
// using MassTransit;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;


// namespace TaxService.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class TaxController : ControllerBase
// {
//     private readonly IMediator _mediator;

//     public TaxController(IMediator mediator)
//     {
//         _mediator = mediator;
//     }

//     [HttpPost]
//     public async Task<IActionResult> Create([FromBody] TaxDto command)
//     {

//         var commands = new CreateTaxCommand(command);
//         var result = await _mediator.Send(commands);
//         return CreatedAtAction(nameof(GetById), new { id = result }, result);
//     }

//     [HttpGet("{id}")]
//     public async Task<IActionResult> GetById(Guid id)
//     {
//         var result = await _mediator.Send(new GetTaxByIdQuery(id));
//         return result is not null ? Ok(result) : NotFound();
//     }

//     [HttpGet]
//     public async Task<IActionResult> GetAll()
//     {
//         var result = await _mediator.Send(new GetAllTaxesQuery());
//         return Ok(result);
//     }

//     [HttpPut("{id}")]
//     public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxCommand command)
//     {
//         if (id != command.Id) return BadRequest();
//         var result = await _mediator.Send(command);
//         return result ? NoContent() : NotFound();
//     }

//     [HttpDelete("{id}")]
//     public async Task<IActionResult> Delete(Guid id)
//     {
//         var result = await _mediator.Send(new DeleteTaxCommand(id));
//         return result ? NoContent() : NotFound();
//     }
// }
