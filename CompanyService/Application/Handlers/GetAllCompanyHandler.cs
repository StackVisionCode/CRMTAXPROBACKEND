
using AutoMapper;
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
using CompanyService.Domains;
using CompanyService.Infraestructure.Commands;
using CompanyService.Infraestructure.Queries;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Application.Handlers;
public class GetAllCompanyHandler : IRequestHandler<GetAllCompanyQueries, ApiResponse<List<CompanyDto>>>
{


    private readonly CompanyDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllCompanyHandler> _logger;


    public GetAllCompanyHandler(CompanyDbContext context , IMapper mapper , ILogger<GetAllCompanyHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }


    public async Task<ApiResponse<List<CompanyDto>>> Handle(GetAllCompanyQueries request, CancellationToken cancellationToken)
    {
        try
        {
            var companies = await _context.Companies
                .Where(c => c.IActive == true)
                .ToListAsync(cancellationToken);

            if (companies == null || companies.Count == 0)
            {
                return new ApiResponse<List<CompanyDto>>(false, "No companies found", null!);
            }

            var companyDtos = _mapper.Map<List<CompanyDto>>(companies);

            return new ApiResponse<List<CompanyDto>>(true, "Companies retrieved successfully", companyDtos);
            
        }
        catch (Exception ex)
        {
            
            _logger?.LogError(ex, "An error occurred while retrieving companies");
            return new ApiResponse<List<CompanyDto>>(false, "An error occurred while retrieving companies", null!);
        }
    }
}
