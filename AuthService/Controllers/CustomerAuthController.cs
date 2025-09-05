using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Applications.Common.Utils;
using AuthService.DTOs.SessionDTOs;
using Commands.CustomerCommands;
using Commands.SessionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CustomerQueries;
using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth/customer")]
public class CustomerAuthController : ControllerBase
{
    private readonly IMediator _med;
    private readonly IHttpClientFactory _http;

    public CustomerAuthController(IMediator med, IHttpClientFactory http)
    {
        _med = med;
        _http = http;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login(
        [FromBody] CustomerLoginRequestDTO dto
    )
    {
        var ip = IpAddressHelper.GetClientIp(HttpContext);
        var agent = Request.Headers.UserAgent.ToString();
        var command = new CustomerLoginCommand(dto, ip, agent);
        var result = await _med.Send(command);
        if (!(result?.Success ?? false))
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<ReadCustomerSessionDTO>>>> GetOwn()
    {
        var rawId =
            User.FindFirstValue("customer_id") ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(rawId, out var customerId))
            return Unauthorized(
                new ApiResponse<List<ReadCustomerSessionDTO>>(false, "Token inválido")
            );

        var query = new GetCustomerSessionsQuery(customerId);
        var result = await _med.Send(query);
        return Ok(result);
    }

    [HasPermission("Customer.SelfRead")]
    [HttpGet("profile")]
    public async Task<ApiResponse<RemoteProfileDTO>> Profile()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var customerId))
            return new(false, "Token inválido");

        var c = _http.CreateClient("Customers");
        var resp = await c.GetAsync($"/api/ContactInfo/Internal/Profile?customerId={customerId}");
        if (!resp.IsSuccessStatusCode)
            return new(false, "No profile");

        var wrapper = await resp.Content.ReadFromJsonAsync<ApiResponse<RemoteProfileDTO>>();
        return wrapper ?? new(false, "Error");
    }

    [HttpPost("logout")]
    public async Task<ApiResponse<bool>> Logout()
    {
        var sid = User.FindFirst("sid")?.Value;
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sid, out var sessionId) || !Guid.TryParse(sub, out var customerId))
            return new(false, "Token inválido");

        var command = new CustomerLogoutCommand(sessionId, customerId);
        var result = await _med.Send(command);
        return result;
    }

    [HttpPost("logout-all")]
    public async Task<ApiResponse<bool>> LogoutAll()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var customerId))
            return new(false, "Token inválido");

        var command = new CustomerLogoutAllCommand(customerId);
        var result = await _med.Send(command);
        return result;
    }
}
