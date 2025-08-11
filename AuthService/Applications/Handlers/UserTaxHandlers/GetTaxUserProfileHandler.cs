using Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserCompanyDTOs;
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
            // 🔍 PASO 1: Determinar si es TaxUser o UserCompany
            var userType = await DetermineUserTypeAsync(request.UserId, cancellationToken);

            if (userType == null)
            {
                return new ApiResponse<UserProfileDTO>(false, "User not found", null!);
            }

            UserProfileDTO profile;

            if (userType == "TaxUser")
            {
                profile = await GetTaxUserProfileAsync(request.UserId, cancellationToken);
            }
            else // UserCompany
            {
                profile = await GetUserCompanyAsProfileAsync(request.UserId, cancellationToken);
            }

            _logger.LogInformation(
                "User profile retrieved successfully: {UserId} ({UserType})",
                request.UserId,
                userType
            );

            return new ApiResponse<UserProfileDTO>(
                true,
                "User profile retrieved successfully",
                profile
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

    /// <summary>
    /// 🔍 Determina si el UserId pertenece a TaxUser o UserCompany
    /// </summary>
    private async Task<string?> DetermineUserTypeAsync(Guid userId, CancellationToken ct)
    {
        var isTaxUser = await _db.TaxUsers.AnyAsync(u => u.Id == userId, ct);
        if (isTaxUser)
            return "TaxUser";

        var isUserCompany = await _db.UserCompanies.AnyAsync(uc => uc.Id == userId, ct);
        if (isUserCompany)
            return "UserCompany";

        return null;
    }

    /// <summary>
    /// 👤 Obtiene perfil de TaxUser (Administrador)
    /// </summary>
    private async Task<UserProfileDTO> GetTaxUserProfileAsync(Guid userId, CancellationToken ct)
    {
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
            join ccountry in _db.Countries on ca.CountryId equals ccountry.Id into companyCountries
            from ccountry in companyCountries.DefaultIfEmpty()
            join cstate in _db.States on ca.StateId equals cstate.Id into companyStates
            from cstate in companyStates.DefaultIfEmpty()
            where u.Id == userId
            select new UserProfileDTO
            {
                Id = u.Id,
                CompanyId = u.CompanyId,
                Email = u.Email,
                Name = u.Name,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                PhotoUrl = u.PhotoUrl,
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
                CompanyFullName = c != null ? c.FullName : null,
                CompanyName = c != null ? c.CompanyName : null,
                CompanyBrand = c != null ? c.Brand : null,
                CompanyIsIndividual = c != null ? !c.IsCompany : false,
                CompanyDomain = c != null ? c.Domain : null,
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

        var user = await userQuery.FirstOrDefaultAsync(ct);
        if (user == null)
            throw new InvalidOperationException("TaxUser not found");

        // Obtener roles
        var rolesQuery =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.TaxUserId == userId
            select r.Name;

        user.RoleNames = await rolesQuery.ToListAsync(ct);
        return user;
    }

    /// <summary>
    /// 🏢 Obtiene UserCompany como UserProfileDTO (adaptado)
    /// </summary>
    private async Task<UserProfileDTO> GetUserCompanyAsProfileAsync(
        Guid userId,
        CancellationToken ct
    )
    {
        var userQuery =
            from uc in _db.UserCompanies
            join c in _db.Companies on uc.CompanyId equals c.Id into companies
            from c in companies.DefaultIfEmpty()
            join a in _db.Addresses on uc.AddressId equals a.Id into addresses
            from a in addresses.DefaultIfEmpty()
            join country in _db.Countries on a.CountryId equals country.Id into countries
            from country in countries.DefaultIfEmpty()
            join state in _db.States on a.StateId equals state.Id into states
            from state in states.DefaultIfEmpty()
            join ca in _db.Addresses on c.AddressId equals ca.Id into companyAddresses
            from ca in companyAddresses.DefaultIfEmpty()
            join ccountry in _db.Countries on ca.CountryId equals ccountry.Id into companyCountries
            from ccountry in companyCountries.DefaultIfEmpty()
            join cstate in _db.States on ca.StateId equals cstate.Id into companyStates
            from cstate in companyStates.DefaultIfEmpty()
            where uc.Id == userId
            select new UserProfileDTO
            {
                Id = uc.Id,
                CompanyId = uc.CompanyId,
                Email = uc.Email,
                Name = uc.Name,
                LastName = uc.LastName,
                PhoneNumber = uc.PhoneNumber,
                PhotoUrl = uc.PhotoUrl,
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
                CompanyFullName = c != null ? c.FullName : null,
                CompanyName = c != null ? c.CompanyName : null,
                CompanyBrand = c != null ? c.Brand : null,
                CompanyIsIndividual = c != null ? !c.IsCompany : false,
                CompanyDomain = c != null ? c.Domain : null,
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

        var user = await userQuery.FirstOrDefaultAsync(ct);
        if (user == null)
            throw new InvalidOperationException("UserCompany not found");

        // Obtener roles de UserCompany
        var rolesQuery =
            from ucr in _db.UserCompanyRoles
            join r in _db.Roles on ucr.RoleId equals r.Id
            where ucr.UserCompanyId == userId
            select r.Name;

        user.RoleNames = await rolesQuery.ToListAsync(ct);
        return user;
    }
}
