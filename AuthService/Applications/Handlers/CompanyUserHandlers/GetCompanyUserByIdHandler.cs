using AuthService.DTOs.CompanyUserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetCompanyUserByIdHandler
    : IRequestHandler<GetCompanyUserByIdQuery, ApiResponse<CompanyUserGetDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetCompanyUserByIdHandler> _logger;

    public GetCompanyUserByIdHandler(
        ApplicationDbContext db,
        ILogger<GetCompanyUserByIdHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyUserGetDTO>> Handle(
        GetCompanyUserByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyUser = await _db
                .CompanyUsers.Where(cu => cu.Id == request.CompanyUserId)
                .Include(cu => cu.CompanyUserProfile)
                .Include(cu => cu.CompanyUserRoles)
                .ThenInclude(cur => cur.Role)
                .Select(cu => new CompanyUserGetDTO
                {
                    Id = cu.Id,
                    CompanyId = cu.CompanyId,
                    Email = cu.Email,
                    Name = cu.CompanyUserProfile.Name,
                    LastName = cu.CompanyUserProfile.LastName,
                    Position = cu.CompanyUserProfile.Position,
                    IsActive = cu.IsActive,
                    CreatedAt = cu.CreatedAt,
                    RoleNames = cu.CompanyUserRoles.Select(cur => cur.Role.Name).ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (companyUser is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<CompanyUserGetDTO>(false, "Company user not found");
            }

            _logger.LogInformation(
                "Company user retrieved successfully: {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<CompanyUserGetDTO>(
                true,
                "Company user retrieved successfully",
                companyUser
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company user {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<CompanyUserGetDTO>(false, "Error retrieving company user");
        }
    }
}
