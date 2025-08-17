using Application.Common.DTO;
using Common;
using Infrastructure.Commands;
using Infrastructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailsController(IMediator mediator) => _mediator = mediator;

    // Crear email (pendiente)
    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmailDTO>>> Create([FromBody] CreateEmailDTO dto)
    {
        try
        {
            var created = await _mediator.Send(
                new CreateEmailCommand(dto, dto.CompanyId, dto.CreatedByTaxUserId)
            );
            var response = new ApiResponse<EmailDTO>(true, "Email created successfully", created);
            return CreatedAtRoute(
                "GetEmailById",
                new { id = created.Id, companyId = dto.CompanyId },
                response
            );
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<EmailDTO>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Update
    [HttpPut("{id:Guid}")]
    public async Task<ActionResult<ApiResponse<EmailDTO>>> Update(
        Guid id,
        [FromBody] UpdateEmailDTO dto
    )
    {
        try
        {
            var updated = await _mediator.Send(
                new UpdateEmailCommand(id, dto, dto.CompanyId, dto.LastModifiedByTaxUserId)
            );
            var response = new ApiResponse<EmailDTO>(true, "Email updated successfully", updated);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            var response = new ApiResponse<EmailDTO>(false, "Email not found", null);
            return NotFound(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<EmailDTO>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Delete
    [HttpDelete("{id:Guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id,
        [FromQuery] Guid companyId,
        [FromQuery] Guid deletedByTaxUserId
    )
    {
        try
        {
            await _mediator.Send(new DeleteEmailCommand(id, companyId, deletedByTaxUserId));
            var response = new ApiResponse<object>(true, "Email deleted successfully", null);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            var response = new ApiResponse<object>(false, "Email not found", null);
            return NotFound(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<object>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Send
    [HttpPost("{id:Guid}/send")]
    public async Task<ActionResult<ApiResponse<EmailDTO>>> Send(
        Guid id,
        [FromQuery] Guid companyId,
        [FromQuery] Guid sentByTaxUserId
    )
    {
        try
        {
            var sent = await _mediator.Send(new SendEmailCommand(id, companyId, sentByTaxUserId));
            var response = new ApiResponse<EmailDTO>(true, "Email sent successfully", sent);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<EmailDTO>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Listar emails con paginación
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmailDTO>>>> List(
        [FromQuery] Guid companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? taxUserId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null
    )
    {
        try
        {
            var result = await _mediator.Send(
                new GetEmailsWithPaginationQuery(
                    companyId,
                    page,
                    pageSize,
                    taxUserId,
                    status,
                    fromDate,
                    toDate
                )
            );
            var response = new ApiResponse<PagedResult<EmailDTO>>(
                true,
                "Emails retrieved successfully",
                result
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<PagedResult<EmailDTO>>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Obtener emails por estado
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmailDTO>>>> GetByStatus(
        string status,
        [FromQuery] Guid companyId,
        [FromQuery] Guid? taxUserId = null
    )
    {
        try
        {
            if (!Enum.TryParse<EmailServices.Domain.EmailStatus>(status, true, out var emailStatus))
            {
                var errorResponse = new ApiResponse<IEnumerable<EmailDTO>>(
                    false,
                    "Invalid status",
                    null
                );
                return BadRequest(errorResponse);
            }

            var result = await _mediator.Send(
                new GetEmailsByStatusQuery(emailStatus, companyId, taxUserId)
            );
            var response = new ApiResponse<IEnumerable<EmailDTO>>(
                true,
                "Emails retrieved successfully",
                result
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<IEnumerable<EmailDTO>>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    // Detalle de email
    [HttpGet("{id:Guid}", Name = "GetEmailById")]
    public async Task<ActionResult<ApiResponse<EmailDTO>>> Get(Guid id, [FromQuery] Guid companyId)
    {
        try
        {
            var mail = await _mediator.Send(new GetEmailByIdQuery(id, companyId));
            if (mail is null)
            {
                var notFoundResponse = new ApiResponse<EmailDTO>(false, "Email not found", null);
                return NotFound(notFoundResponse);
            }

            var response = new ApiResponse<EmailDTO>(true, "Email retrieved successfully", mail);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<EmailDTO>(false, ex.Message, null);
            return BadRequest(response);
        }
    }
}
