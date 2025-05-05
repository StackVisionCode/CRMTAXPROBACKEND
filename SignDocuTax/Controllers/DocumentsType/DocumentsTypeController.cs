

using Commands.DocumentsType;
using Common;
using DTOs.DocumentsType;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.Documents;

namespace Controllers.DocumentsType;

[ApiController]
[Route("api/[controller]")]
public class DocumentsTypeController : ControllerBase
{
  private readonly IMediator _mediator;
  public DocumentsTypeController(IMediator mediator)
  {
    _mediator = mediator;
  }


  [HttpPost("Create")]
  public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateNewDocumentsTypeDTo documentsTypeDto)
  {
    // Mapeas el DTO al Command (usando AutoMapper)
    var command = new CreateDocumentTypeCommands(documentsTypeDto);
    var result = await _mediator.Send(command);
    if (result == null) return BadRequest(new { message = "Failed to create Documents Type" });


    return Ok(result);
  }



  [HttpGet("GetAll")]
  public async Task<ActionResult<ApiResponse<ReadDocumentsType[]>>> GetAll()
  {
    var result = await _mediator.Send(new GetAllDocumentsTypeQuery());

    if (result.Success == false) return BadRequest(new { result });

    return Ok(result);


  }


  [HttpPut("Update")]
  public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] UpdateDocumentsTypeDTo documentsTypeDto)
  {
    var command = new UpdateDocumentTypeCommands(documentsTypeDto);
    var result = await _mediator.Send(command);

    if (!result.Success.GetValueOrDefault()) return BadRequest(new { result });


    return Ok(result);
  }

  [HttpDelete("Delete")]
  public async Task<ActionResult<ApiResponse<bool>>> Delete(DeleteDocumentsTypeDTo id)
  {
    var command = new DeleteDocumentTypeCommands(id);
    var result = await _mediator.Send(command);

    if (!result.Success.GetValueOrDefault()) return BadRequest(result);

    return Ok(result);
  }


[HttpGet("getById/{id}")]
public async Task<ActionResult<ApiResponse<ReadDocumentsType>>> GetById([FromRoute] int id)
{
    var query = new GetDocumentsTypeByIdQuery(new ReadDocumentsTypeById { Id = id });
    var result = await _mediator.Send(query);

    if (!result.Success.GetValueOrDefault()) return BadRequest(result);
    return Ok(result);
}




}
