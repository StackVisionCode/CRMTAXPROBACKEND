using AuthService.DTOs.CompanyPermissionDTOs;
using Common;
using MediatR;

namespace Commands.CompanyPermissionCommands;

/// <summary>
/// Asignar un permiso personalizado a un TaxUser (Administrator → User)
/// </summary>
public record AssignCompanyPermissionCommand(AssignCompanyPermissionDTO CompanyPermissionData)
    : IRequest<ApiResponse<CompanyPermissionDTO>>;

/// <summary>
/// Actualizar un CompanyPermission existente (Administrator)
/// </summary>
public record UpdateCompanyPermissionCommand(UpdateCompanyPermissionDTO CompanyPermissionData)
    : IRequest<ApiResponse<CompanyPermissionDTO>>;

/// <summary>
/// Eliminar un CompanyPermission (Administrator)
/// </summary>
public record RemoveCompanyPermissionCommand(Guid CompanyPermissionId)
    : IRequest<ApiResponse<bool>>;

/// <summary>
/// Revocar/Otorgar un CompanyPermission (Administrator)
/// Cambia el estado IsGranted
/// </summary>
public record ToggleCompanyPermissionCommand(Guid CompanyPermissionId, bool IsGranted)
    : IRequest<ApiResponse<CompanyPermissionDTO>>;

/// <summary>
/// Asignar múltiples permisos a un TaxUser (Administrator)
/// </summary>
public record BulkAssignCompanyPermissionsCommand(
    Guid TaxUserId,
    ICollection<AssignCompanyPermissionDTO> Permissions
) : IRequest<ApiResponse<IEnumerable<CompanyPermissionDTO>>>;

/// <summary>
/// Revocar múltiples permisos de un TaxUser (Administrator)
/// </summary>
public record BulkRevokeCompanyPermissionsCommand(
    Guid TaxUserId,
    ICollection<string> PermissionCodes
) : IRequest<ApiResponse<bool>>;
