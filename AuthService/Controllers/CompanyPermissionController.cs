using AuthService.DTOs.CompanyPermissionDTOs;
using AuthService.DTOs.PermissionDTOs;
using Commands.CompanyPermissionCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CompanyPermissionQueries;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyPermissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyPermissionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Asignar permiso personalizado a TaxUser (Administrator → User)
    /// </summary>
    [HttpPost("Assign")]
    public async Task<ActionResult<ApiResponse<CompanyPermissionDTO>>> AssignCompanyPermission(
        [FromBody] AssignCompanyPermissionDTO companyPermissionDto
    )
    {
        var command = new AssignCompanyPermissionCommand(companyPermissionDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CompanyPermission por ID
    /// </summary>
    [HttpGet("GetById/{companyPermissionId}")]
    public async Task<ActionResult<ApiResponse<CompanyPermissionDTO>>> GetCompanyPermissionById(
        Guid companyPermissionId
    )
    {
        var query = new GetCompanyPermissionByIdQuery(companyPermissionId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CompanyPermissions por Company ID (Administrator/Developer)
    /// </summary>
    [HttpGet("GetByCompany/{companyId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CompanyPermissionDTO>>>
    > GetCompanyPermissionsByCompany(Guid companyId, [FromQuery] bool? isGranted = null)
    {
        var query = new GetCompanyPermissionsByCompanyQuery(companyId, isGranted);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Obtener CompanyPermissions por TaxUser ID
    /// </summary>
    [HttpGet("GetByTaxUser/{taxUserId}")]
    public async Task<
        ActionResult<ApiResponse<CompanyUserPermissionsDTO>>
    > GetCompanyPermissionsByTaxUser(Guid taxUserId, [FromQuery] bool? isGranted = null)
    {
        var query = new GetCompanyPermissionsByTaxUserQuery(taxUserId, isGranted);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener permisos efectivos de un TaxUser
    /// Combina permisos de roles + permisos personalizados
    /// </summary>
    [HttpGet("GetEffectivePermissions/{taxUserId}")]
    public async Task<
        ActionResult<ApiResponse<CompanyUserPermissionsDTO>>
    > GetEffectivePermissionsByTaxUser(Guid taxUserId)
    {
        var query = new GetEffectivePermissionsByTaxUserQuery(taxUserId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los CompanyPermissions (Solo Developer)
    /// </summary>
    [HttpGet("GetAll")]
    // [Authorize(Roles = "Developer")] // Descomentar cuando tengas auth por roles
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CompanyPermissionDTO>>>
    > GetAllCompanyPermissions([FromQuery] bool? isGranted = null)
    {
        var query = new GetAllCompanyPermissionsQuery(isGranted);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Verificar si un TaxUser tiene un permiso específico
    /// </summary>
    [HttpGet("CheckPermission/{taxUserId}/{permissionCode}")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckTaxUserPermission(
        Guid taxUserId,
        string permissionCode
    )
    {
        var query = new CheckTaxUserPermissionQuery(taxUserId, permissionCode);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Actualizar CompanyPermission (Administrator/Developer)
    /// </summary>
    [HttpPut("Update")]
    public async Task<ActionResult<ApiResponse<CompanyPermissionDTO>>> UpdateCompanyPermission(
        [FromBody] UpdateCompanyPermissionDTO companyPermissionDto
    )
    {
        var command = new UpdateCompanyPermissionCommand(companyPermissionDto);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Remover CompanyPermission (Administrator/Developer)
    /// </summary>
    [HttpDelete("Remove/{companyPermissionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveCompanyPermission(
        Guid companyPermissionId
    )
    {
        var command = new RemoveCompanyPermissionCommand(companyPermissionId);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Revocar/Otorgar CompanyPermission (Administrator/Developer)
    /// </summary>
    [HttpPatch("Toggle/{companyPermissionId}")]
    public async Task<ActionResult<ApiResponse<CompanyPermissionDTO>>> ToggleCompanyPermission(
        Guid companyPermissionId,
        [FromBody] bool isGranted
    )
    {
        var command = new ToggleCompanyPermissionCommand(companyPermissionId, isGranted);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Asignar múltiples permisos a un TaxUser (Administrator/Developer)
    /// </summary>
    [HttpPost("BulkAssign")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<CompanyPermissionDTO>>>
    > BulkAssignCompanyPermissions([FromBody] BulkAssignCompanyPermissionsRequest request)
    {
        var command = new BulkAssignCompanyPermissionsCommand(
            request.TaxUserId,
            request.Permissions
        );
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Revocar múltiples permisos de un TaxUser (Administrator/Developer)
    /// </summary>
    [HttpDelete("BulkRevoke/{taxUserId}")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkRevokeCompanyPermissions(
        Guid taxUserId,
        [FromBody] ICollection<string> permissionCodes
    )
    {
        var command = new BulkRevokeCompanyPermissionsCommand(taxUserId, permissionCodes);
        var result = await _mediator.Send(command);

        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// NUEVO: Obtener estadísticas de permisos por Company
    /// </summary>
    [HttpGet("GetStats/{companyId}")]
    public async Task<
        ActionResult<ApiResponse<CompanyPermissionStatsDTO>>
    > GetCompanyPermissionStats(Guid companyId)
    {
        var query = new GetCompanyPermissionStatsQuery(companyId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// NUEVO: Obtener permisos disponibles para asignar a un TaxUser
    /// </summary>
    [HttpGet("GetAvailablePermissions/{taxUserId}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<PermissionDTO>>>
    > GetAvailablePermissionsForTaxUser(Guid taxUserId)
    {
        var query = new GetAvailablePermissionsForTaxUserQuery(taxUserId);
        var result = await _mediator.Send(query);

        if (result.Success == false)
            return NotFound(result);

        return Ok(result);
    }
}
