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
            // JOIN EXPLÍCITO: Una sola query optimizada para obtener todos los datos necesarios
            var companyUser = await (
                from cu in _db.CompanyUsers
                join cup in _db.CompanyUserProfiles
                    on cu.Id equals cup.CompanyUserId
                    into profileGroup
                from profile in profileGroup.DefaultIfEmpty() // LEFT JOIN para manejar usuarios sin perfil
                where cu.Id == request.CompanyUserId
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
                    // Los roles se obtendrán en una consulta separada para evitar duplicación de datos
                    RoleNames = new List<string>(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (companyUser is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<CompanyUserGetDTO>(false, "Company user not found");
            }

            // CONSULTA SEPARADA: Obtener roles para evitar el problema N+1 y duplicación
            var roleNames = await (
                from cur in _db.CompanyUserRoles
                join r in _db.Roles on cur.RoleId equals r.Id
                where cur.CompanyUserId == request.CompanyUserId
                select r.Name
            ).ToListAsync(cancellationToken);

            // Asignar los roles al DTO
            companyUser.RoleNames = roleNames;

            _logger.LogInformation(
                "Company user retrieved successfully: {CompanyUserId} with {RoleCount} roles",
                request.CompanyUserId,
                roleNames.Count
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
                "Error retrieving company user {CompanyUserId}: {Message}",
                request.CompanyUserId,
                ex.Message
            );
            return new ApiResponse<CompanyUserGetDTO>(false, "Error retrieving company user");
        }
    }
}
