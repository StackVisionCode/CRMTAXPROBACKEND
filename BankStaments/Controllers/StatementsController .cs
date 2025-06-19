using BankStaments.Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BankStaments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StatementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadStatement(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var command = new ProcessStatementCommand(stream, file.FileName);

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStatement(Guid id)
        {
            var query = new GetStatementQuery(id);
            var result = await _mediator.Send(query);
            
            return result != null ? Ok(result) : NotFound();
        }

        // [HttpGet("{id}/transactions")]
        // public async Task<IActionResult> GetTransactions(Guid id, [FromQuery] string? type = null)
        // {
        //     var query = new GetTransactionsQuery 
        //     { 
        //         StatementId = id,
        //         TransactionType = type
        //     };
            
        //     var result = await _mediator.Send(query);
        //     return Ok(result);
        // }

        // [HttpGet("{id}/summary")]
        // public async Task<IActionResult> GetSummary(int id)
        // {
        //     var query = new GetSummaryQuery { StatementId = id };
        //     var result = await _mediator.Send(query);
            
        //     return result != null ? Ok(result) : NotFound();
        // }

        // [HttpPost("export")]
        // public async Task<IActionResult> ExportStatement([FromBody] ExportStatementCommand command)
        // {
        //     var result = await _mediator.Send(command);
            
        //     return result.Format switch
        //     {
        //         "PDF" => File(result.Content, "application/pdf", result.FileName),
        //         "Excel" => File(result.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName),
        //         "CSV" => File(result.Content, "text/csv", result.FileName),
        //         _ => BadRequest("Unsupported export format")
        //     };
        // }
    }
}