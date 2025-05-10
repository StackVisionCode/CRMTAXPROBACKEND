
using AutoMapper;
using CompanyService.Application.Commons;
using CompanyService.Domains;
using CompanyService.Infraestructure.Commands;
using Infraestructure.Context;
using MediatR;

namespace CompanyService.Application.Handlers;

public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommands, ApiResponse<bool>>
{
  
    private readonly CompanyDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCompanyHandler> _logger;
 
    public UpdateCompanyHandler(IMapper mapper, CompanyDbContext context, ILogger<UpdateCompanyHandler> logger )
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;
      
    }

    public async Task<ApiResponse<bool>> Handle(UpdateCompanyCommands request, CancellationToken cancellationToken)
    {
            try
            {
                if (request.CompanyDto == null)
                {
                    return  new ApiResponse<bool>(false, "Company data is null",false);
                }
                var company = await _context.Companies.FindAsync(new object[] { request.CompanyDto.Id }, cancellationToken);
                if (company == null)
                {
                    return new ApiResponse<bool>(false, "Company not found", false);
                }
                _mapper.Map(request.CompanyDto, company);
                company.UpdatedAt = DateTime.UtcNow;
                _context.Companies.Update(company);
                bool result = await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
                if (!result)
                {
                    _logger?.LogError("Failed to update company: {Company}", request.CompanyDto);
                    // Log the error or handle it as needed
                    return new ApiResponse<bool>(false, "Failed to update company", false);
                }
                _logger?.LogInformation("Company updated successfully: {Company}", request.CompanyDto);
                return new ApiResponse<bool>(true, "Company updated successfully", true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while updating the company: {Company}", request.CompanyDto);
                return new ApiResponse<bool>(false, "An error occurred while updating the company", false);
            }
    }
}
