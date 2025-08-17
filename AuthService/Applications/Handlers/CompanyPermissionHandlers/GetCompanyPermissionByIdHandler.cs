using AuthService.DTOs.CompanyPermissionDTOs;
using AuthService.DTOs.PermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// <summary>
/// Handler para obtener CompanyPermission por ID
/// </summary>
public class GetCompanyPermissionByIdHandler
    : IRequestHandler<GetCompanyPermissionByIdQuery, ApiResponse<CompanyPermissionDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyPermissionByIdHandler> _logger;

    public GetCompanyPermissionByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyPermissionByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPermissionDTO>> Handle(
        GetCompanyPermissionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyPermissionQuery = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.Id == request.CompanyPermissionId
                select new CompanyPermissionDTO
                {
                    Id = cp.Id,
                    TaxUserId = cp.TaxUserId,
                    PermissionId = cp.PermissionId,
                    IsGranted = cp.IsGranted,
                    Description = cp.Description,
                    UserEmail = tu.Email,
                    UserName = tu.Name,
                    UserLastName = tu.LastName,
                    PermissionCode = p.Code,
                    PermissionName = p.Name,
                    CreatedAt = cp.CreatedAt,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (companyPermissionQuery == null)
            {
                _logger.LogWarning(
                    "CompanyPermission not found: {CompanyPermissionId}",
                    request.CompanyPermissionId
                );
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "CompanyPermission not found",
                    null!
                );
            }

            return new ApiResponse<CompanyPermissionDTO>(
                true,
                "CompanyPermission retrieved successfully",
                companyPermissionQuery
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CompanyPermission: {CompanyPermissionId}",
                request.CompanyPermissionId
            );
            return new ApiResponse<CompanyPermissionDTO>(
                false,
                "Error retrieving CompanyPermission",
                null!
            );
        }
    }
}
