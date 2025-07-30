using Applications.DTOs.CompanyDTOs;
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
            // Consulta principal del usuario con direcciones
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                join a in _dbContext.Addresses on u.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                join ca in _dbContext.Addresses on c.AddressId equals ca.Id into companyAddresses
                from ca in companyAddresses.DefaultIfEmpty()
                join ccountry in _dbContext.Countries
                    on ca.CountryId equals ccountry.Id
                    into companyCountries
                from ccountry in companyCountries.DefaultIfEmpty()
                join cstate in _dbContext.States on ca.StateId equals cstate.Id into companyStates
                from cstate in companyStates.DefaultIfEmpty()
                where u.Id == request.Id
                select new UserGetDTO
                {
                    Id = u.Id,
                    CompanyId = u.CompanyId,
                    Email = u.Email,
                    Name = u.Name,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    PhotoUrl = u.PhotoUrl,
                    IsActive = u.IsActive,
                    Confirm = u.Confirm ?? false,
                    CreatedAt = u.CreatedAt,
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
                return new ApiResponse<UserGetDTO>(false, "User not found", null!);
            }

            // Obtener roles del usuario
            var rolesQuery =
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == request.Id
                select r.Name;

            var roles = await rolesQuery.ToListAsync(cancellationToken);
            user.RoleNames = roles;

            _logger.LogInformation("User retrieved successfully: {UserId}", request.Id);
            return new ApiResponse<UserGetDTO>(true, "User retrieved successfully", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {Id}: {Message}", request.Id, ex.Message);
            return new ApiResponse<UserGetDTO>(false, ex.Message, null!);
        }
    }
}
