using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetCompanyByIdHandler : IRequestHandler<GetCompanyByIdQuery, ApiResponse<CompanyDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyByIdHandler> _logger;

    public GetCompanyByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyDTO>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyQuery =
                from c in _dbContext.Companies
                join a in _dbContext.Addresses on c.AddressId equals a.Id into addresses
                from a in addresses.DefaultIfEmpty()
                join country in _dbContext.Countries on a.CountryId equals country.Id into countries
                from country in countries.DefaultIfEmpty()
                join state in _dbContext.States on a.StateId equals state.Id into states
                from state in states.DefaultIfEmpty()
                where c.Id == request.Id
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

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                return new ApiResponse<CompanyDTO>(false, "Company not found", null!);
            }

            return new ApiResponse<CompanyDTO>(true, "Company retrieved successfully", company);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company {Id}: {Message}",
                request.Id,
                ex.Message
            );
            return new ApiResponse<CompanyDTO>(false, ex.Message, null!);
        }
    }
}
