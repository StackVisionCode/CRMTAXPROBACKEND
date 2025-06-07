using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class UpdateCompanyTaxHandler : IRequestHandler<UpdateTaxCompanyCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCompanyTaxHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IPasswordHash _passwordHash;

    public UpdateCompanyTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCompanyTaxHandler> logger,
        IMapper mapper,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateTaxCompanyCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar la compañía existente
            var userCompany = await _dbContext
                .TaxUsers.Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == request.CompanyTax.Id, cancellationToken);

            if (userCompany == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyTax.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            // Verificar si el email ya existe en otro usuario (si se está cambiando)
            if (!string.IsNullOrEmpty(request.CompanyTax.Email))
            {
                var emailExists = await _dbContext.TaxUsers.AnyAsync(u =>
                    u.Email == request.CompanyTax.Email
                );

                if (emailExists)
                {
                    _logger.LogWarning("Email already exists: {Email}", request.CompanyTax.Email);
                    return new ApiResponse<bool>(false, "Email already exists", false);
                }
            }

            // 1. Campos de TaxUser
            if (!string.IsNullOrWhiteSpace(request.CompanyTax.Email))
                userCompany.Email = request.CompanyTax.Email;

            if (!string.IsNullOrWhiteSpace(request.CompanyTax.Domain))
                userCompany.Domain = request.CompanyTax.Domain;

            if (request.CompanyTax.IsActive.HasValue)
                userCompany.IsActive = request.CompanyTax.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(request.CompanyTax.Password))
                userCompany.Password = _passwordHash.HashPassword(request.CompanyTax.Password);

            userCompany.UpdatedAt = DateTime.UtcNow;

            // Actualizar datos de la compañía
            if (userCompany.Company != null)
            {
                userCompany.Company.FullName = request.CompanyTax.FullName;
                userCompany.Company.CompanyName = request.CompanyTax.CompanyName;
                userCompany.Company.Address = request.CompanyTax.Address;
                userCompany.Company.Phone = request.CompanyTax.Phone;
                userCompany.Company.Description = request.CompanyTax.Description;
                userCompany.Company.UserLimit = request.CompanyTax.UserLimit;
                userCompany.Company.Brand = request.CompanyTax.Brand;
                userCompany.Company.UpdatedAt = DateTime.UtcNow;
            }

            // Guardamos los cambios
            _dbContext.TaxUsers.Update(userCompany);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Company updated successfully: {CompanyId}", userCompany.Id);
                return new ApiResponse<bool>(true, "Company updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update company: {CompanyId}", request.CompanyTax.Id);
                return new ApiResponse<bool>(false, "Failed to update company", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating company: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
