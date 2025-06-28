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
            var user = await (
                from u in _db.TaxUsers
                where u.Id == request.UserId
                join p in _db.TaxUserProfiles on u.Id equals p.TaxUserId
                join c in _db.Companies on u.CompanyId equals c.Id into cs
                from c in cs.DefaultIfEmpty()
                join ur in _db.UserRoles on u.Id equals ur.TaxUserId into urs
                from ur in urs.DefaultIfEmpty()
                join r in _db.Roles on ur.RoleId equals r.Id into rs
                from r in rs.DefaultIfEmpty()

                group new
                {
                    u,
                    p,
                    c,
                    r,
                } by new
                {
                    u,
                    p,
                    c,
                } into g
                select new UserProfileDTO
                {
                    Id = g.Key.u.Id,
                    Email = g.Key.u.Email,
                    Domain = g.Key.u.Domain,
                    Name = g.Key.p.Name,
                    LastName = g.Key.p.LastName,
                    Address = g.Key.p.Address,
                    PhotoUrl = g.Key.p.PhotoUrl,
                    CompanyId = g.Key.u.CompanyId ?? Guid.Empty,
                    FullName = g.Key.c != null ? g.Key.c.FullName : null,
                    CompanyName = g.Key.c != null ? g.Key.c.CompanyName : null,
                    CompanyBrand = g.Key.c != null ? g.Key.c.Brand : null,
                    RoleNames = g.Where(x => x.r != null)
                        .Select(x => x.r!.Name)
                        .Distinct()
                        .ToList(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

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
