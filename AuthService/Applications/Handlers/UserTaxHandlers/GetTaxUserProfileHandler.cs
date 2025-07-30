using Applications.DTOs.CompanyDTOs;
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
            // Consulta principal con direcciones
            var userQuery =
                from u in _db.TaxUsers
                join c in _db.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                join a in _db.Addresses on u.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _db.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _db.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                join ca in _db.Addresses on c.AddressId equals ca.Id into companyAddresses
                from ca in companyAddresses.DefaultIfEmpty()
                join ccountry in _db.Countries
                    on ca.CountryId equals ccountry.Id
                    into companyCountries
                from ccountry in companyCountries.DefaultIfEmpty()
                join cstate in _db.States on ca.StateId equals cstate.Id into companyStates
                from cstate in companyStates.DefaultIfEmpty()
                where u.Id == request.UserId
                select new UserProfileDTO
                {
                    Id = u.Id,
                    CompanyId = u.CompanyId,
                    Email = u.Email,
                    Name = u.Name,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    PhotoUrl = u.PhotoUrl,
                    // User Address
                    Address =
                        a != null
                            ? new AddressDTO
                            {
                                CountryId = a.CountryId,
                                StateId = a.StateId,
                                City = a.City,
                                Street = a.Street,
                                Line = a.Line,
                                ZipCode = a.ZipCode,
                                CountryName = country.Name,
                                StateName = state.Name,
                            }
                            : null,
                    // Company info
                    CompanyFullName = c != null ? c.FullName : null,
                    CompanyName = c != null ? c.CompanyName : null,
                    CompanyBrand = c != null ? c.Brand : null,
                    CompanyIsIndividual = c != null ? !c.IsCompany : false,
                    CompanyDomain = c != null ? c.Domain : null,
                    // Company Address
                    CompanyAddress =
                        ca != null
                            ? new AddressDTO
                            {
                                CountryId = ca.CountryId,
                                StateId = ca.StateId,
                                City = ca.City,
                                Street = ca.Street,
                                Line = ca.Line,
                                ZipCode = ca.ZipCode,
                                CountryName = ccountry.Name,
                                StateName = cstate.Name,
                            }
                            : null,
                    RoleNames = new List<string>(),
                };

            var user = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                return new ApiResponse<UserProfileDTO>(false, "User not found", null!);
            }

            // Obtener roles del usuario
            var rolesQuery =
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == request.UserId
                select r.Name;

            var roles = await rolesQuery.ToListAsync(cancellationToken);
            user.RoleNames = roles;

            _logger.LogInformation("User profile retrieved successfully: {UserId}", request.UserId);
            return new ApiResponse<UserProfileDTO>(
                true,
                "User profile retrieved successfully",
                user
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error reading profile for {UserId}: {Message}",
                request.UserId,
                ex.Message
            );
            return new ApiResponse<UserProfileDTO>(false, "Internal error", null!);
        }
    }
}
