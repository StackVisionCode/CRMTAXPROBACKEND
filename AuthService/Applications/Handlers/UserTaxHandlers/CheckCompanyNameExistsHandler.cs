using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class CheckCompanyNameExistsHandler
    : IRequestHandler<CheckCompanyNameExistsQuery, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CheckCompanyNameExistsHandler> _logger;

    public CheckCompanyNameExistsHandler(
        ApplicationDbContext dbContext,
        ILogger<CheckCompanyNameExistsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CheckCompanyNameExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return new ApiResponse<bool>(true, "Company name is empty", false);
            }

            var normalizedCompanyName = request.CompanyName.Trim().ToLowerInvariant();

            var existsQuery =
                from c in _dbContext.Companies
                where
                    c.IsCompany == true
                    && c.CompanyName != null
                    && c.CompanyName.ToLower().Trim() == normalizedCompanyName
                    && (request.ExcludeCompanyId == null || c.Id != request.ExcludeCompanyId)
                select c.Id;

            var exists = await existsQuery.AnyAsync(cancellationToken);

            _logger.LogInformation(
                "Company name '{CompanyName}' exists check: {Exists}",
                request.CompanyName,
                exists
            );

            return new ApiResponse<bool>(
                true,
                exists ? "Company name already exists" : "Company name is available",
                exists
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if company name exists: {CompanyName} - {Message}",
                request.CompanyName,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
