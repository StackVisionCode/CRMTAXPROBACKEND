
using Commands.DocumentsStatus;
using Commands.DocumentsType;
using Common;
using DTOs.DocumentsStatus;
using DTOs.DocumentsType;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.DocumentStatus;

namespace Controllers.DocumentsStatus;

[ApiController]
[Route("api/[controller]")]
public class DocumentsStatusController : ControllerBase
{
  private readonly IMediator _mediator;
  public DocumentsStatusController(IMediator mediator)
  {
    _mediator = mediator;
  }


  [HttpPost("Create")]
  public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateNewDocumentsStatusDtos documentsTypeDto)
  {
    // Mapeas el DTO al Command (usando AutoMapper)
    var command = new CreateDocumentStatusCommands(documentsTypeDto);
    var result = await _mediator.Send(command);
    if (result == null) return BadRequest(new { message = "Failed to create Documents Type" });


    return Ok(result);
  }


  [HttpGet("GetAll")]
  public async Task<ActionResult<ApiResponse<ReadDocumentsDtosStatus[]>>> GetAll()
  {
    var result = await _mediator.Send(new GetAllDocumentsStatusQuery());

    if (result.Success == false) return BadRequest(new { result });

    return Ok(result);


  }

  [HttpPut("Update")]
  public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] CreateNewDocumentsStatusDtos documentsTypeDto)
  {
    var command = new UpdateDocumentStatusCommands(documentsTypeDto);
    var result = await _mediator.Send(command);

    if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });


    return Ok(result);
  }

  [HttpDelete("Delete")]
  public async Task<ActionResult<ApiResponse<bool>>> Delete(DeleteDocumentsStatusDTo id)
  {
    var command = new DeleteDocumenttStatusCommands(id);
    var result = await _mediator.Send(command);

    if (!result.Success.GetValueOrDefault()) return BadRequest(result);

    return Ok(result);
  }



  




}
