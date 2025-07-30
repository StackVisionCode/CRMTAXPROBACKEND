using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyHandlers;

public class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCompanyHandler> _logger;

    public DeleteCompanyHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // ✅ MEJORADO: Buscar company con más información
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == request.Id
                select new
                {
                    Company = c,
                    UsersCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    HasAddress = c.AddressId.HasValue,
                    AddressId = c.AddressId,
                };

            var companyData = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyData?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            var company = companyData.Company;

            // ✅ MEJORADO: Validaciones más completas
            if (companyData.UsersCount > 0)
            {
                _logger.LogWarning(
                    "Cannot delete company {CompanyId} - has {UserCount} associated users",
                    request.Id,
                    companyData.UsersCount
                );
                return new ApiResponse<bool>(
                    false,
                    $"Cannot delete company with {companyData.UsersCount} associated users. Delete users first.",
                    false
                );
            }

            // ✅ NUEVO: Verificar otras dependencias (sessions, etc.)
            var sessionsCountQuery =
                from s in _dbContext.Sessions
                join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                where u.CompanyId == request.Id
                select s.Id;

            var sessionsCount = await sessionsCountQuery.CountAsync(cancellationToken);
            if (sessionsCount > 0)
            {
                _logger.LogWarning(
                    "Company {CompanyId} has {SessionCount} active sessions",
                    request.Id,
                    sessionsCount
                );
                return new ApiResponse<bool>(
                    false,
                    "Cannot delete company with active user sessions. Please try again later.",
                    false
                );
            }

            // ✅ NUEVO: Eliminar dirección de la company si existe
            if (companyData.HasAddress && companyData.AddressId.HasValue)
            {
                var addressQuery =
                    from a in _dbContext.Addresses
                    where a.Id == companyData.AddressId.Value
                    select a;

                var address = await addressQuery.FirstOrDefaultAsync(cancellationToken);
                if (address != null)
                {
                    _dbContext.Addresses.Remove(address);
                    _logger.LogDebug(
                        "Marked company address for deletion: {AddressId}",
                        address.Id
                    );
                }
            }

            // ✅ Eliminar la company
            _dbContext.Companies.Remove(company);

            // ✅ Guardar todos los cambios de una vez
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company deleted successfully: CompanyId={CompanyId}, AddressDeleted={AddressDeleted}",
                    request.Id,
                    companyData.HasAddress
                );
                return new ApiResponse<bool>(true, "Company deleted successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete company: {CompanyId}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete company", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error deleting company {CompanyId}: {Message}",
                request.Id,
                ex.Message
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while deleting the company",
                false
            );
        }
    }
}
