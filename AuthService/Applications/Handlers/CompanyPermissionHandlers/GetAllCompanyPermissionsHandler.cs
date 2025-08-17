using AuthService.DTOs.CompanyPermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para obtener todos los CompanyPermissions
public class GetAllCompanyPermissionsHandler
    : IRequestHandler<GetAllCompanyPermissionsQuery, ApiResponse<IEnumerable<CompanyPermissionDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllCompanyPermissionsHandler> _logger;

    public GetAllCompanyPermissionsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllCompanyPermissionsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CompanyPermissionDTO>>> Handle(
        GetAllCompanyPermissionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyPermissionsQuery = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where (request.IsGranted == null || cp.IsGranted == request.IsGranted)
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
                "All CompanyPermissions retrieved successfully",
                companyPermissionsQuery
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all CompanyPermissions");
            return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                false,
                "Error retrieving all CompanyPermissions",
                null!
            );
        }
    }
}
