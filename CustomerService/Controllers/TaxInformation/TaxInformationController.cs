using Common;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.DTOs.TaxInformationDTOs;
using CustomerService.Queries.TaxInformationQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers.TaxInformation
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxInformationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaxInformationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create(
            [FromBody] CreateTaxInformationDTOs taxInformationDto
        )
        {
            var command = new CreateTaxInformationCommands(taxInformationDto);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create a taxInformation" });
            return Ok(result);
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(
            [FromBody] UpdateTaxInformationDTOs taxInformationDto
        )
        {
            var command = new UpdateTaxInformationCommands(taxInformationDto);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteTaxInformationCommands(id);
            var result = await _mediator.Send(command);

            if (result?.Success != true)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            var command = new GetAllTaxInformationQueries();
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult> GetById(Guid Id)
        {
            var command = new GetByIdTaxInformationQueries(Id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
