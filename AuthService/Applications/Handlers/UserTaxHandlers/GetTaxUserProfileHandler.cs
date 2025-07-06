using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserHandlers;

public class GetTaxUserProfileHandler
    : IRequestHandler<GetTaxUserProfileQuery, ApiResponse<UserProfileDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTaxUserProfileHandler> _logger;

    public GetTaxUserProfileHandler(
        ApplicationDbContext db,
        IMapper mapper,
        ILogger<GetTaxUserProfileHandler> logger
    )
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDTO>> Handle(
        GetTaxUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // ────────────────────────────────────────────────────────────────
            // 1. Traemos TODA la info en una única consulta
            // ────────────────────────────────────────────────────────────────
            var rows = await (
                from u in _db.TaxUsers
                where u.Id == request.UserId
                join p in _db.TaxUserProfiles on u.Id equals p.TaxUserId
                join c0 in _db.Companies on u.CompanyId equals c0.Id into cs
                from c in cs.DefaultIfEmpty()
                join ur in _db.UserRoles on u.Id equals ur.TaxUserId into urs
                from ur in urs.DefaultIfEmpty()
                join r0 in _db.Roles on ur.RoleId equals r0.Id into rs
                from r in rs.DefaultIfEmpty()
                select new
                {
                    u, // entidad usuario
                    p, // perfil
                    c, // compañía (nullable)
                    RoleName = r != null ? r.Name : null,
                }
            ).ToListAsync(cancellationToken);

            if (!rows.Any())
                return new ApiResponse<UserProfileDTO>(false, "User not found");

            // ────────────────────────────────────────────────────────────────
            // 2. Armamos el DTO en memoria (¡sin GroupBy en SQL!)
            // ────────────────────────────────────────────────────────────────
            var first = rows.First(); // todas las filas comparten u/p/c
            var user = new UserProfileDTO
            {
                Id = first.u.Id,
                Email = first.u.Email,
                Domain = first.u.Domain,
                Name = first.p.Name,
                LastName = first.p.LastName,
                Address = first.p.Address,
                PhotoUrl = first.p.PhotoUrl,
                CompanyId = first.u.CompanyId ?? Guid.Empty,
                FullName = first.c?.FullName,
                CompanyName = first.c?.CompanyName,
                CompanyBrand = first.c?.Brand,
                RoleNames = rows.Where(x => x.RoleName != null)
                    .Select(x => x.RoleName!)
                    .Distinct()
                    .ToList(),
            };

            if (user is null)
                return new(false, "User not found");

            var dto = _mapper.Map<UserProfileDTO>(user);
            return new(true, "Ok", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading profile for {UserId}", request.UserId);
            return new(false, "Internal error");
        }
    }
}
