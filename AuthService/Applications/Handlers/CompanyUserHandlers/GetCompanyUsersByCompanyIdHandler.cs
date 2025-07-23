using AuthService.DTOs.CompanyUserDTOs;
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
    private readonly ILogger<GetCompanyUsersByCompanyIdHandler> _logger;

    public GetCompanyUsersByCompanyIdHandler(
        ApplicationDbContext db,
        ILogger<GetCompanyUsersByCompanyIdHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CompanyUserGetDTO>>> Handle(
        GetCompanyUsersByCompanyIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // PASO 1: JOIN EXPLÍCITO para obtener usuarios con sus perfiles
            var users = await (
                from cu in _db.CompanyUsers
                join cup in _db.CompanyUserProfiles
                    on cu.Id equals cup.CompanyUserId
                    into profileGroup
                from profile in profileGroup.DefaultIfEmpty() // LEFT JOIN para usuarios sin perfil
                where cu.CompanyId == request.CompanyId
                select new CompanyUserGetDTO
                {
                    Id = cu.Id,
                    CompanyId = cu.CompanyId,
                    Email = cu.Email,
                    Name = profile != null ? profile.Name : null,
                    LastName = profile != null ? profile.LastName : null,
                    Position = profile != null ? profile.Position : null,
                    IsActive = cu.IsActive,
                    CreatedAt = cu.CreatedAt,
                    RoleNames = new List<string>(), // Se llenará después
                }
            ).ToListAsync(cancellationToken);

            if (!users.Any())
            {
                _logger.LogInformation(
                    "No company users found for company {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<List<CompanyUserGetDTO>>(
                    true,
                    "No company users found",
                    new List<CompanyUserGetDTO>()
                );
            }

            // PASO 2: JOIN EXPLÍCITO para obtener todos los roles de los usuarios en una sola consulta
            var userIds = users.Select(u => u.Id).ToList();

            var userRoles = await (
                from cur in _db.CompanyUserRoles
                join r in _db.Roles on cur.RoleId equals r.Id
                where userIds.Contains(cur.CompanyUserId)
                select new { UserId = cur.CompanyUserId, RoleName = r.Name }
            ).ToListAsync(cancellationToken);

            // PASO 3: Agrupar roles por usuario y asignarlos eficientemente
            var rolesByUser = userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ur => ur.RoleName).ToList());

            // PASO 4: Asignar roles a cada usuario
            foreach (var user in users)
            {
                user.RoleNames = rolesByUser.ContainsKey(user.Id)
                    ? rolesByUser[user.Id]
                    : new List<string>();
            }

            _logger.LogInformation(
                "Retrieved {UserCount} company users for company {CompanyId} with a total of {RoleCount} role assignments",
                users.Count,
                request.CompanyId,
                userRoles.Count
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
                "Error retrieving company users for company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<List<CompanyUserGetDTO>>(
                false,
                "Error retrieving company users"
            );
        }
    }
}
