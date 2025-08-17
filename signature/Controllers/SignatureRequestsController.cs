using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.DTOs;
using Infrastruture.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using signature.Application.DTOs;
using signature.Infrastruture.Commands;
using signature.Infrastruture.Queries;

[ApiController]
[Route("api/[controller]")]
public class SignatureRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SignatureRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("requests")]
    public async Task<ActionResult> Create([FromBody] CreateSignatureRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var companyId = GetCompanyIdFromClaims();
            var taxUserId = GetTaxUserIdFromClaims();

            if (companyId == Guid.Empty || taxUserId == Guid.Empty)
            {
                return Unauthorized("Invalid user context");
            }

            var command = new CreateSignatureRequestCommand(dto, companyId, taxUserId);
            var result = await _mediator.Send(command);

            if (result.Success == true)
                return CreatedAtAction(
                    nameof(ValidateToken),
                    new { token = "placeholder" },
                    result
                );

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{token}")] // validar
    public async Task<ActionResult> ValidateToken(string token)
    {
        var result = await _mediator.Send(new ValidateTokenQuery(token));
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var command = new GetSignatureRequestsQuery();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var command = new GetSignatureRequestDetailQuery(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{id:guid}/signers")]
    public async Task<ActionResult> GetSignersByRequestId(Guid id)
    {
        var command = new GetSignersByRequestQuery(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("layout/{token}")]
    public async Task<ActionResult> GetLayout(string token)
    {
        var result = await _mediator.Send(new GetSigningLayoutQuery(token));
        return Ok(result);
    }

    [HttpPost("consent")]
    public async Task<ActionResult> RegisterConsent([FromBody] RegisterConsentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new RegisterConsentCommand(dto));
        if (result.Success == true)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPost("submit")]
    public async Task<ActionResult> Submit(SignDocumentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new SubmitSignatureCommand(dto));
        return Ok(result);
    }

    [HttpPost("reject")]
    public async Task<ActionResult> Reject([FromBody] RejectSignatureDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var result = await _mediator.Send(new RejectSignatureCommand(dto));
        return Ok(result);
    }

    // Métodos auxiliares para extraer información de los claims
    private Guid GetCompanyIdFromClaims()
    {
        var companyIdClaim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : Guid.Empty;
    }

    private Guid GetTaxUserIdFromClaims()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
