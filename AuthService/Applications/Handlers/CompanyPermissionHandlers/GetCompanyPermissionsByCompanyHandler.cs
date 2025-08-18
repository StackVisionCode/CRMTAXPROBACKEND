using AuthService.DTOs.CompanyPermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para obtener CompanyPermissions por Company
public class GetCompanyPermissionsByCompanyHandler
    : IRequestHandler<
        GetCompanyPermissionsByCompanyQuery,
        ApiResponse<IEnumerable<CompanyPermissionDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyPermissionsByCompanyHandler> _logger;

    public GetCompanyPermissionsByCompanyHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyPermissionsByCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CompanyPermissionDTO>>> Handle(
        GetCompanyPermissionsByCompanyQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyPermissionsQuery = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where
                    tu.CompanyId == request.CompanyId
                    && (request.IsGranted == null || cp.IsGranted == request.IsGranted)
                orderby tu.Email, p.Code
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
            ).ToListAsync(cancellationToken);

            return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                true,
                "CompanyPermissions by company retrieved successfully",
                companyPermissionsQuery
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CompanyPermissions by company: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                false,
                "Error retrieving CompanyPermissions by company",
                null!
            );
        }
    }
}
