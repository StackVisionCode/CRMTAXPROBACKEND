using AuthService.DTOs.CompanyUserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetCompanyUserProfileHandler
    : IRequestHandler<GetCompanyUserProfileQuery, ApiResponse<CompanyUserProfileDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyUserProfileHandler> _logger;

    public GetCompanyUserProfileHandler(
        ApplicationDbContext db,
        IMapper mapper,
        ILogger<GetCompanyUserProfileHandler> logger
    )
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyUserProfileDTO>> Handle(
        GetCompanyUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Consulta optimizada con todas las navegaciones necesarias
            var rows = await (
                from cu in _db.CompanyUsers
                where cu.Id == request.CompanyUserId
                join cup in _db.CompanyUserProfiles on cu.Id equals cup.CompanyUserId
                join c in _db.Companies on cu.CompanyId equals c.Id
                join cur in _db.CompanyUserRoles on cu.Id equals cur.CompanyUserId into curs
                from cur in curs.DefaultIfEmpty()
                join r in _db.Roles on cur.RoleId equals r.Id into rs
                from r in rs.DefaultIfEmpty()
                select new
                {
                    CompanyUser = cu,
                    Profile = cup,
                    Company = c,
                    RoleName = r != null ? r.Name : null,
                }
            ).ToListAsync(cancellationToken);

            if (!rows.Any())
            {
                _logger.LogWarning(
                    "Company user profile not found: {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<CompanyUserProfileDTO>(false, "Company user not found");
            }

            // Armar el DTO en memoria
            var first = rows.First();
            var profileDto = new CompanyUserProfileDTO
            {
                Id = first.CompanyUser.Id,
                Email = first.CompanyUser.Email,
                Name = first.Profile.Name,
                LastName = first.Profile.LastName,
                Address = first.Profile.Address,
                PhotoUrl = first.Profile.PhotoUrl,
                Position = first.Profile.Position,
                CompanyId = first.CompanyUser.CompanyId,
                CompanyName = first.Company?.CompanyName,
                CompanyBrand = first.Company?.Brand,
                CompanyFullName = first.Company?.FullName,
                RoleNames = rows.Where(x => x.RoleName != null)
                    .Select(x => x.RoleName!)
                    .Distinct()
                    .ToList(),
            };

            _logger.LogInformation(
                "Company user profile retrieved successfully: {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<CompanyUserProfileDTO>(
                true,
                "Profile retrieved successfully",
                profileDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company user profile {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<CompanyUserProfileDTO>(false, "Error retrieving profile");
        }
    }
}
