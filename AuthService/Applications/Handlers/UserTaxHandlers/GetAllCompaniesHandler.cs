using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetAllCompaniesHandler
    : IRequestHandler<GetAllCompaniesQuery, ApiResponse<List<CompanyDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllCompaniesHandler> _logger;

    public GetAllCompaniesHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllCompaniesHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CompanyDTO>>> Handle(
        GetAllCompaniesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companiesQuery =
                from c in _dbContext.Companies
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                select new CompanyDTO
                {
                    Id = c.Id,
                    IsCompany = c.IsCompany,
                    FullName = c.FullName,
                    CompanyName = c.CompanyName,
                    Brand = c.Brand,
                    Phone = c.Phone,
                    Description = c.Description,
                    Domain = c.Domain,
                    UserLimit = c.UserLimit,
                    CurrentUserCount = c.TaxUsers.Count(),
                    CreatedAt = c.CreatedAt,
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
                };

            var companies = await companiesQuery.ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} companies successfully", companies.Count);
            return new ApiResponse<List<CompanyDTO>>(
                true,
                "Companies retrieved successfully",
                companies
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies: {Message}", ex.Message);
            return new ApiResponse<List<CompanyDTO>>(false, ex.Message, new List<CompanyDTO>());
        }
    }
}
