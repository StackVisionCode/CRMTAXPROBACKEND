using Common;
using DTOs.PublicDTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Queries.PublicQueries;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("PublicApiPolicy")] // Rate limiting a nivel de controlador
public class PublicController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PublicController> _logger;

    public PublicController(IMediator mediator, ILogger<PublicController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene información pública limitada de un TaxUser para chat/video
    /// Rate Limited: 100 requests per minute per IP
    /// </summary>
    /// <param name="taxUserId">ID del TaxUser</param>
    /// <returns>Información básica del usuario y compañía</returns>
    [HttpGet("taxuser/{taxUserId:guid}")]
    [EnableRateLimiting("StrictPublicPolicy")] // Rate limiting más estricto
    [ProducesResponseType(typeof(ApiResponse<TaxUserPublicInfoDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<TaxUserPublicInfoDTO>>> GetTaxUserPublicInfo(
        Guid taxUserId
    )
    {
        try
        {
            if (taxUserId == Guid.Empty)
            {
                _logger.LogWarning("Invalid TaxUser ID provided: {TaxUserId}", taxUserId);
                return BadRequest(new ApiResponse<object>(false, "Invalid user ID", new object()));
            }

            var query = new GetTaxUserPublicInfoQuery(taxUserId);
            var result = await _mediator.Send(query);

            if (result.Success == null || !result.Success.Value)
            {
                return BadRequest(result);
            }

            // Log para auditoría de acceso público
            _logger.LogInformation(
                "Public TaxUser info accessed: {TaxUserId} from IP: {ClientIP}",
                taxUserId,
                GetClientIpAddress()
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error in GetTaxUserPublicInfo for {TaxUserId}",
                taxUserId
            );
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error", new object())
            );
        }
    }

    /// <summary>
    /// Obtiene información pública limitada de una Company para chat/video
    /// Rate Limited: 100 requests per minute per IP
    /// </summary>
    /// <param name="companyId">ID de la Company</param>
    /// <returns>Información básica de la compañía y owner</returns>
    [HttpGet("company/{companyId:guid}")]
    [EnableRateLimiting("StrictPublicPolicy")]
    [ProducesResponseType(typeof(ApiResponse<CompanyPublicInfoDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<CompanyPublicInfoDTO>>> GetCompanyPublicInfo(
        Guid companyId
    )
    {
        try
        {
            if (companyId == Guid.Empty)
            {
                _logger.LogWarning("Invalid Company ID provided: {CompanyId}", companyId);
                return BadRequest(
                    new ApiResponse<object>(false, "Invalid company ID", new object())
                );
            }

            var query = new GetCompanyPublicInfoQuery(companyId);
            var result = await _mediator.Send(query);

            if (result.Success == null || !result.Success.Value)
            {
                return BadRequest(result);
            }

            // Log para auditoría de acceso público
            _logger.LogInformation(
                "Public Company info accessed: {CompanyId} from IP: {ClientIP}",
                companyId,
                GetClientIpAddress()
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error in GetCompanyPublicInfo for {CompanyId}",
                companyId
            );
            return StatusCode(
                500,
                new ApiResponse<object>(false, "Internal server error", new object())
            );
        }
    }

    private string GetClientIpAddress()
    {
        // Obtener IP del cliente para logging y rate limiting
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Check for forwarded IP (load balancer/proxy)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = Request
                .Headers["X-Forwarded-For"]
                .FirstOrDefault()
                ?.Split(',')
                .FirstOrDefault()
                ?.Trim();
        }
        else if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "unknown";
    }
}
