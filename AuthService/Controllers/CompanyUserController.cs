using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.DTOs.CompanyUserDTOs;
using AuthService.DTOs.CompanyUserSessionDTOs;
using AuthService.DTOs.RoleDTOs;
using Commands.CompanyUserCommands;
using Common;
using MediatR;
//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Queries.CompanyUserQueries;

//using SharedLibrary.Authorizations;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //[HasPermission("TaxUser.Create")]
    [HttpPost("Create")]
    public async Task<ActionResult<ApiResponse<bool>>> Create(
        [FromBody] NewCompanyUserDTO userDto,
        [FromHeader(Name = "Origin")] string origin
    )
    {
        var command = new CreateCompanyUserCommand(userDto, origin);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //[HasPermission("TaxUser.Update")]
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(
        [FromBody] UpdateCompanyUserDTO userDto
    )
    {
        var command = new UpdateCompanyUserCommand(userDto);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //[HasPermission("TaxUser.Delete")]
    [HttpDelete("Delete/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var command = new DeleteCompanyUserCommand(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //[HasPermission("TaxUser.Read")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyUserGetDTO>>> GetById(Guid id)
    {
        var query = new GetCompanyUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    //[HasPermission("TaxUser.Read")]
    [HttpGet("Company/{companyId:guid}")]
    public async Task<ActionResult<ApiResponse<List<CompanyUserGetDTO>>>> GetByCompany(
        Guid companyId
    )
    {
        var query = new GetCompanyUsersByCompanyIdQuery(companyId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("Profile")]
    public async Task<ActionResult<ApiResponse<CompanyUserProfileDTO>>> GetProfile()
    {
        var idRaw =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(idRaw, out var userId))
            return Unauthorized(new ApiResponse<CompanyUserProfileDTO>(false, "Invalid session"));

        var command = new GetCompanyUserProfileQuery(userId);
        var result = await _mediator.Send(command);

        // Enriquecer con datos del JWT si están vacíos en BD (similar a TaxUserController)
        if (result.Success.HasValue && result.Success.Value && result.Data != null)
        {
            var profileDto = result.Data;

            if (string.IsNullOrWhiteSpace(profileDto.Name))
                profileDto.Name = User.FindFirst(ClaimTypes.GivenName)?.Value;

            if (string.IsNullOrWhiteSpace(profileDto.LastName))
                profileDto.LastName = User.FindFirst(ClaimTypes.Surname)?.Value;

            if (string.IsNullOrWhiteSpace(profileDto.CompanyFullName))
                profileDto.CompanyFullName = User.FindFirst("fullName")?.Value;

            if (string.IsNullOrWhiteSpace(profileDto.CompanyName))
                profileDto.CompanyName = User.FindFirst("companyName")?.Value;

            if (string.IsNullOrWhiteSpace(profileDto.CompanyBrand))
                profileDto.CompanyBrand = User.FindFirst("companyBrand")?.Value;
        }

        return Ok(result);
    }

    //[HasPermission("RolePermission.Create")]
    [HttpPost("{companyUserId:guid}/roles/{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignRole(Guid companyUserId, Guid roleId)
    {
        var command = new AssignRoleToCompanyUserCommand(companyUserId, roleId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //[HasPermission("RolePermission.Delete")]
    [HttpDelete("{companyUserId:guid}/roles/{roleId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveRole(Guid companyUserId, Guid roleId)
    {
        var command = new RemoveRoleFromCompanyUserCommand(companyUserId, roleId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    //[HasPermission("Role.Read")]
    [HttpGet("{companyUserId:guid}/roles")]
    public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetRoles(Guid companyUserId)
    {
        var query = new GetRolesByCompanyUserIdQuery(companyUserId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    //[HasPermission("Sessions.Read")]
    [HttpGet("{companyUserId:guid}/sessions")]
    public async Task<ActionResult<ApiResponse<List<ReadCompanyUserSessionDTO>>>> GetSessions(
        Guid companyUserId
    )
    {
        var query = new GetCompanyUserSessionsQuery(companyUserId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    //[HasPermission("Sessions.Read")]
    [HttpGet("{companyUserId:guid}/sessions/active")]
    public async Task<ActionResult<ApiResponse<List<CompanyUserSessionDTO>>>> GetActiveSessions(
        Guid companyUserId
    )
    {
        var query = new GetActiveCompanyUserSessionsQuery(companyUserId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
