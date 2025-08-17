using AuthService.DTOs.CompanyPermissionDTOs;
using AuthService.DTOs.PermissionDTOs;
using Common;
using MediatR;

namespace Queries.CompanyPermissionQueries;

/// <summary>
/// Obtener CompanyPermission por ID
/// </summary>
public record GetCompanyPermissionByIdQuery(Guid CompanyPermissionId)
    : IRequest<ApiResponse<CompanyPermissionDTO>>;

/// <summary>
/// Obtener todos los CompanyPermissions por Company (Administrator/Developer)
/// </summary>
public record GetCompanyPermissionsByCompanyQuery(Guid CompanyId, bool? IsGranted = null)
    : IRequest<ApiResponse<IEnumerable<CompanyPermissionDTO>>>;

/// <summary>
/// Obtener CompanyPermissions por TaxUser específico
/// </summary>
public record GetCompanyPermissionsByTaxUserQuery(Guid TaxUserId, bool? IsGranted = null)
    : IRequest<ApiResponse<CompanyUserPermissionsDTO>>;

/// <summary>
/// Obtener permisos efectivos de un TaxUser
/// Combina permisos de roles + permisos personalizados
/// </summary>
public record GetEffectivePermissionsByTaxUserQuery(Guid TaxUserId)
    : IRequest<ApiResponse<CompanyUserPermissionsDTO>>;

/// <summary>
/// Obtener todos los CompanyPermissions (Solo Developer)
/// </summary>
public record GetAllCompanyPermissionsQuery(bool? IsGranted = null)
    : IRequest<ApiResponse<IEnumerable<CompanyPermissionDTO>>>;

/// <summary>
/// Verificar si un TaxUser tiene un permiso específico
/// </summary>
public record CheckTaxUserPermissionQuery(Guid TaxUserId, string PermissionCode)
    : IRequest<ApiResponse<bool>>;

/// <summary>
/// NUEVO: Obtener estadísticas de permisos por Company
/// </summary>
public record GetCompanyPermissionStatsQuery(Guid CompanyId)
    : IRequest<ApiResponse<CompanyPermissionStatsDTO>>;

/// <summary>
/// NUEVO: Obtener permisos disponibles para asignar (que no tiene el TaxUser)
/// </summary>
public record GetAvailablePermissionsForTaxUserQuery(Guid TaxUserId)
    : IRequest<ApiResponse<IEnumerable<PermissionDTO>>>;
