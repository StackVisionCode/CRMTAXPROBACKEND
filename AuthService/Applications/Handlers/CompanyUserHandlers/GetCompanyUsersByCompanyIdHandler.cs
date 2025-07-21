using AuthService.DTOs.CompanyUserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetCompanyUsersByCompanyIdHandler
    : IRequestHandler<GetCompanyUsersByCompanyIdQuery, ApiResponse<List<CompanyUserGetDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyUsersByCompanyIdHandler> _logger;

    public GetCompanyUsersByCompanyIdHandler(
        ApplicationDbContext db,
        IMapper mapper,
        ILogger<GetCompanyUsersByCompanyIdHandler> logger
    )
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CompanyUserGetDTO>>> Handle(
        GetCompanyUsersByCompanyIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var users = await _db
                .CompanyUsers.Where(cu => cu.CompanyId == request.CompanyId)
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
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} company users for company {CompanyId}",
                users.Count,
                request.CompanyId
            );
            return new ApiResponse<List<CompanyUserGetDTO>>(
                true,
                "Company users retrieved successfully",
                users
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company users for company {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<List<CompanyUserGetDTO>>(
                false,
                "Error retrieving company users"
            );
        }
    }
}
