
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
public class GetCompanyByIdHandler : IRequestHandler<GetCompanyByIdQueries, ApiResponse<CompanyDto>>
{

    private readonly CompanyDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyByIdHandler> _logger;
    public GetCompanyByIdHandler(CompanyDbContext context, IMapper mapper, ILogger<GetCompanyByIdHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyDto>> Handle(GetCompanyByIdQueries request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Id <= 0)
            {
                return new ApiResponse<CompanyDto>(false, "Invalid company ID", null!);
            }

            var company = await _context.Companies
                .Where(c => c.IActive == true && c.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                return new ApiResponse<CompanyDto>(false, "Company not found", null!);
            }

            var companyDto = _mapper.Map<CompanyDto>(company);

            return new ApiResponse<CompanyDto>(true, "Company retrieved successfully", companyDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred while retrieving the company with ID: {CompanyId}", request.Id);
            return new ApiResponse<CompanyDto>(false, "An error occurred while retrieving the company", null!);
            
        }
    }
}

