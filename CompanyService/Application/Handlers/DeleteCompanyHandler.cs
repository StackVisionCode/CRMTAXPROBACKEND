
using CompanyService.Application.Commons;
using CompanyService.Domains;
using CompanyService.Infraestructure.Commands;
using Infraestructure.Context;
using MediatR;

namespace CompanyService.Application.Handlers;
public class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, ApiResponse<bool>>
{


    private readonly CompanyDbContext _context;

    private readonly ILogger<DeleteCompanyHandler> _logger;

    public DeleteCompanyHandler(CompanyDbContext context, ILogger<DeleteCompanyHandler> logger)
    {
        _context = context;
        _logger = logger;

    }
    public async Task<ApiResponse<bool>> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {

            if (request.Id <= 0)
            {
                return new ApiResponse<bool>(false, "Invalid company", false);

            }


            var companyResponse = await GetCompanyByIdAsync(request.Id, cancellationToken);

            if (companyResponse.Success==false)
            {
                return new ApiResponse<bool>(false, companyResponse.Message!, false);
            }
            var company = companyResponse.Data;
            if (company is null)
            {
                return new ApiResponse<bool>(false, "Company not found", false);
            }
            company.IActive = false;
            company.DeletedAt = DateTime.UtcNow;
            _context.Companies.Update(company);
            bool result = await _context.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            if (!result)
            {
                _logger?.LogError("Failed to delete company: {Company}", request.Id);
                // Log the error or handle it as needed
                return new ApiResponse<bool>(false, "Failed to delete company", false);
            }
            _logger?.LogInformation("Company deleted successfully: {Company}", request.Id);
            return new ApiResponse<bool>(true, "Company deleted successfully", true);

        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred while deleting the company: {Company}", request.Id);
            // Log the error or handle it as needed
            return new ApiResponse<bool>(false, "An error occurred while deleting the company", false);

        }
      

    }
    private async Task<ApiResponse<Company>> GetCompanyByIdAsync(int id, CancellationToken cancellationToken)
    {
        var company = await _context.Companies.FindAsync([id], cancellationToken);

        if (company is null)
            return new ApiResponse<Company>(false, "Company not found", null!);

        return new ApiResponse<Company>(true, "Company found", company);
    }

}

