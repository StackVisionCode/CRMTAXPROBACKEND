using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserTaxHandlers;

public class GetTaxUserByIdHandler : IRequestHandler<GetTaxUserByIdQuery, ApiResponse<UserGetDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTaxUserByIdHandler> _logger;

    public GetTaxUserByIdHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetTaxUserByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserGetDTO>> Handle(
        GetTaxUserByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var user = await (
                from u in _dbContext.TaxUsers
                where u.Id == request.Id
                join ur in _dbContext.UserRoles on u.Id equals ur.TaxUserId into urs
                from ur in urs.DefaultIfEmpty()
                join r in _dbContext.Roles on ur.RoleId equals r.Id into rs
                from r in rs.DefaultIfEmpty()
                join p in _dbContext.TaxUserProfiles on u.Id equals p.TaxUserId into ps
                from p in ps.DefaultIfEmpty()
                join c in _dbContext.Companies on u.CompanyId equals c.Id into cs
                from c in cs.DefaultIfEmpty()

                group new
                {
                    r,
                    p,
                    c,
                } by new
                {
                    u.Id,
                    u.CompanyId,
                    u.Email,
                    u.Domain,
                    Name = p != null ? p.Name : null,
                    LastName = p != null ? p.LastName : null,
                    Address = p != null ? p.Address : null,
                    PhotoUrl = p != null ? p.PhotoUrl : null,
                    Phone = p != null ? p.PhoneNumber : null,
                    CompanyName = c != null ? c.CompanyName : null,
                    CompanyBrand = c != null ? c.Brand : null,
                } into g
                select new UserGetDTO
                {
                    Id = g.Key.Id,
                    CompanyId = g.Key.CompanyId,
                    Email = g.Key.Email,
                    Domain = g.Key.Domain,
                    Name = g.Key.Name,
                    LastName = g.Key.LastName,
                    Address = g.Key.Address,
                    PhotoUrl = g.Key.PhotoUrl,
                    Phone = g.Key.Phone,
                    CompanyName = g.Key.CompanyName,
                    CompanyBrand = g.Key.CompanyBrand,
                    FullName =
                        g.Key.Name != null && g.Key.LastName != null
                            ? (g.Key.Name + " " + g.Key.LastName).Trim()
                            : (g.Key.Name ?? g.Key.LastName ?? "").Trim(),
                    RoleNames = g.Where(x => x.r != null)
                        .Select(x => x.r!.Name)
                        .Distinct()
                        .ToList(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                return new(false, "User not found");

            var dto = _mapper.Map<UserGetDTO>(user);

            return new ApiResponse<UserGetDTO>(true, "Ok", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {Id}", request.Id);
            return new(false, ex.Message);
        }
    }
}
