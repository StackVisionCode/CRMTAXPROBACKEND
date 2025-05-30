using Common;
using CustomerService.Commands.AddressCommands;
using CustomerService.DTOs.AddressDTOs;
using CustomerService.Queries.AddressQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.Address
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AddressController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateAddressDTO addressDto)
        {
            var command = new CreateAddressCommands(addressDto);
            var result = await _mediator.Send(command);
            if (result == null) return BadRequest(new { message = "Failed to create an address" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllAddressQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdAddressQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false) return BadRequest(new { result });           

            return Ok(result);
        }
    }
}