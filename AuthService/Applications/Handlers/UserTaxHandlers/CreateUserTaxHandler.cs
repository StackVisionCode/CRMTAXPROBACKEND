using Applications.Common;
using AuthService.Domains.Addresses;
using AuthService.Domains.Users;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.UserTaxHandlers;

public class CreateUserTaxHandler : IRequestHandler<CreateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;
    private readonly IConfirmTokenService _confirmTokenService;
    private readonly LinkBuilder _linkBuilder;
    private readonly IEventBus _eventBus;

    private const int USA = 220;

    public CreateUserTaxHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateUserTaxHandler> logger,
        IPasswordHash passwordHash,
        IEventBus eventBus,
        IConfirmTokenService confirmTokenService,
        LinkBuilder linkBuilder
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHash = passwordHash;
        _eventBus = eventBus;
        _confirmTokenService = confirmTokenService;
        _linkBuilder = linkBuilder;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateTaxUserCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verificar si el usuario ya existe
            var userExistsQuery =
                from u in _dbContext.TaxUsers
                where u.Email == request.UserTax.Email
                select u.Id;

            if (await userExistsQuery.AnyAsync(cancellationToken))
            {
                _logger.LogWarning("User already exists: {Email}", request.UserTax.Email);
                return new ApiResponse<bool>(false, "User already exists", false);
            }

            // Verificar que la company existe y obtener su información
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == request.UserTax.CompanyId
                select new
                {
                    c.Id,
                    c.FullName,
                    c.CompanyName,
                    c.Domain,
                    c.Brand,
                    c.IsCompany,
                    c.UserLimit,
                    CurrentUsers = c.TaxUsers.Count(),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.UserTax.CompanyId);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            // Verificar límite de usuarios
            if (company.CurrentUsers >= company.UserLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded for company: {CompanyId}",
                    request.UserTax.CompanyId
                );
                return new ApiResponse<bool>(false, "User limit exceeded for this company", false);
            }

            // Obtener rol User
            var userRoleQuery =
                from r in _dbContext.Roles
                where r.Name == "User"
                select new { r.Id, r.Name };

            var userRole = await userRoleQuery.FirstOrDefaultAsync(cancellationToken);
            if (userRole == null)
            {
                _logger.LogError("User role not found");
                return new ApiResponse<bool>(false, "User role not found", false);
            }

            Address? userAddressEntity = null;

            if (request.UserTax.Address is not null)
            {
                var addrDto = request.UserTax.Address;
                var validateResult = await ValidateAddressAsync(
                    addrDto.CountryId,
                    addrDto.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                    return new ApiResponse<bool>(false, validateResult.Message, false);

                userAddressEntity = new Address
                {
                    Id = Guid.NewGuid(),
                    CountryId = addrDto.CountryId,
                    StateId = addrDto.StateId,
                    City = addrDto.City?.Trim(),
                    Street = addrDto.Street?.Trim(),
                    Line = addrDto.Line?.Trim(),
                    ZipCode = addrDto.ZipCode?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.Addresses.AddAsync(userAddressEntity, cancellationToken);

                // GUARDAR DIRECCIÓN PRIMERO
                var addressSaved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
                if (!addressSaved)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to save user address");
                    return new ApiResponse<bool>(false, "Failed to save address", false);
                }
                _logger.LogDebug("User address saved with ID: {AddressId}", userAddressEntity.Id);
            }

            // 3. Crear TaxUser con AddressId ya disponible
            var user = _mapper.Map<TaxUser>(request.UserTax);
            user.Id = Guid.NewGuid();
            user.Password = _passwordHash.HashPassword(request.UserTax.Password);
            user.IsActive = false;
            user.Confirm = false;
            user.OtpVerified = false;
            user.CreatedAt = DateTime.UtcNow;

            // CRÍTICO: Asignar AddressId si existe
            if (userAddressEntity != null)
            {
                user.AddressId = userAddressEntity.Id;
                _logger.LogDebug("Assigned user AddressId: {AddressId}", user.AddressId);
            }

            // Generar token de confirmación
            var (token, expiration) = _confirmTokenService.Generate(user.Id, user.Email);
            user.ConfirmToken = token;

            await _dbContext.TaxUsers.AddAsync(user, cancellationToken);

            // Asignar roles (User por defecto + roles adicionales si se especifican)
            var rolesToAssign = new List<Guid> { userRole.Id };
            if (request.UserTax.RoleIds?.Any() == true)
            {
                rolesToAssign.AddRange(request.UserTax.RoleIds.Where(r => r != userRole.Id));
            }

            foreach (var roleId in rolesToAssign.Distinct())
            {
                await _dbContext.UserRoles.AddAsync(
                    new UserRole
                    {
                        Id = Guid.NewGuid(),
                        TaxUserId = user.Id,
                        RoleId = roleId,
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken
                );
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                // Construir payload de address para el evento
                var userAddressPayload = await BuildAddressPayloadAsync(
                    userAddressEntity,
                    cancellationToken
                );

                string link = _linkBuilder.BuildConfirmationLink(request.Origin, user.Email, token);

                _logger.LogInformation(
                    "User created successfully: {UserId} with AddressId: {AddressId}",
                    user.Id,
                    user.AddressId
                );

                // Publicar eventos
                _eventBus.Publish(
                    new UserAddedToCompanyEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        user.Id,
                        user.Email,
                        user.Name ?? string.Empty,
                        user.LastName ?? string.Empty,
                        company.Id,
                        company.FullName,
                        company.CompanyName,
                        company.Domain,
                        company.IsCompany,
                        new[] { "User" }
                    )
                );

                _eventBus.Publish(
                    new AccountConfirmationLinkEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        user.Id,
                        user.Email,
                        $"{user.Name} {user.LastName}".Trim(),
                        link,
                        expiration,
                        company.Id,
                        company.IsCompany,
                        company.FullName,
                        company.CompanyName,
                        $"{user.Name} {user.LastName}".Trim(),
                        company.Domain
                    )
                );

                return new ApiResponse<bool>(true, "User created successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create user");
                return new ApiResponse<bool>(false, "Failed to create user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

        var country = await _dbContext
            .Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == countryId, ct);
        if (country is null)
            return (false, $"CountryId '{countryId}' not found.");

        var state = await _dbContext
            .States.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stateId && s.CountryId == countryId, ct);
        if (state is null)
            return (false, $"StateId '{stateId}' not found for CountryId '{countryId}'.");

        return (true, "OK");
    }

    private async Task<AddressPayload?> BuildAddressPayloadAsync(
        Address? address,
        CancellationToken ct
    )
    {
        if (address is null)
            return null;

        var country = await _dbContext
            .Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == address.CountryId, ct);
        var state = await _dbContext
            .States.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == address.StateId, ct);

        return new AddressPayload(
            CountryId: address.CountryId,
            CountryName: country?.Name ?? string.Empty,
            StateId: address.StateId,
            StateName: state?.Name ?? string.Empty,
            City: address.City?.Trim(),
            Street: address.Street?.Trim(),
            Line: address.Line?.Trim(),
            ZipCode: address.ZipCode?.Trim()
        );
    }
}
