using Applications.Common;
using AuthService.Applications.Common;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Addresses;
using AuthService.Domains.Companies;
using AuthService.Domains.CustomPlans;
using AuthService.Domains.Modules;
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

namespace Handlers.CompanyHandlers;

public class CreateCompanyTaxHandler : IRequestHandler<CreateTaxCompanyCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateCompanyTaxHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _eventBus;
    private readonly LinkBuilder _linkBuilder;
    private readonly IConfirmTokenService _confirmTokenService;

    public CreateCompanyTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateCompanyTaxHandler> logger,
        IMapper mapper,
        IPasswordHash passwordHash,
        IEventBus eventBus,
        IConfirmTokenService confirmTokenService,
        LinkBuilder linkBuilder
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
        _passwordHash = passwordHash;
        _eventBus = eventBus;
        _confirmTokenService = confirmTokenService;
        _linkBuilder = linkBuilder;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateTaxCompanyCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verificar si ya existe el email o domain
            var existsQuery =
                from u in _dbContext.TaxUsers
                where u.Email == request.CompanyTax.Email
                select u.Id;

            var domainExistsQuery =
                from c in _dbContext.Companies
                where c.Domain == request.CompanyTax.Domain
                select c.Id;

            if (await existsQuery.AnyAsync(cancellationToken))
            {
                _logger.LogWarning("Email already exists: {Email}", request.CompanyTax.Email);
                return new ApiResponse<bool>(false, "Email already exists", false);
            }

            if (await domainExistsQuery.AnyAsync(cancellationToken))
            {
                _logger.LogWarning("Domain already exists: {Domain}", request.CompanyTax.Domain);
                return new ApiResponse<bool>(false, "Domain already exists", false);
            }

            // Validación de ServiceLevel
            if (request.CompanyTax.ServiceLevel.HasValue)
            {
                var requestedLevel = request.CompanyTax.ServiceLevel.Value;
                if (!Enum.IsDefined(typeof(ServiceLevel), requestedLevel))
                {
                    _logger.LogWarning(
                        "Invalid ServiceLevel specified: {ServiceLevel}",
                        requestedLevel
                    );
                    return new ApiResponse<bool>(
                        false,
                        "Invalid ServiceLevel. Valid values: 1=Basic, 2=Standard, 3=Pro",
                        false
                    );
                }
            }

            // Determinar ServiceLevel y obtener configuración del servicio
            var serviceLevel = DetermineServiceLevel(request.CompanyTax);
            var adminRoleName = GetAdministratorRoleName(serviceLevel);

            // Obtener configuración del servicio y rol en una consulta
            var serviceConfigQuery =
                from s in _dbContext.Services
                join r in _dbContext.Roles on adminRoleName equals r.Name
                where
                    s.Name == serviceLevel.ToString()
                    && s.IsActive
                    && r.ServiceLevel == serviceLevel
                select new
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Price = s.Price,
                    UserLimit = s.UserLimit,
                    AdminRoleId = r.Id,
                    AdminRoleName = r.Name,
                };

            var serviceConfig = await serviceConfigQuery.FirstOrDefaultAsync(cancellationToken);
            if (serviceConfig == null)
            {
                _logger.LogError(
                    "Service configuration not found for ServiceLevel: {ServiceLevel}",
                    serviceLevel
                );
                return new ApiResponse<bool>(
                    false,
                    $"Service configuration not found for {serviceLevel}",
                    false
                );
            }

            _logger.LogInformation(
                "Creating company with ServiceLevel: {ServiceLevel}, Role: {RoleName}",
                serviceLevel,
                serviceConfig.AdminRoleName
            );

            // Generar IDs únicos
            var companyId = Guid.NewGuid();
            var adminUserId = Guid.NewGuid();

            // Lógica de direcciones según el tipo de cuenta
            Address? companyAddressEntity = null;
            Address? adminAddressEntity = null;

            // Para EMPRESAS: crear ambas direcciones si existen
            if (request.CompanyTax.IsCompany)
            {
                // Dirección de la empresa
                if (request.CompanyTax.Address is not null)
                {
                    var addrDto = request.CompanyTax.Address;
                    var validateResult = await ValidateAddressAsync(
                        addrDto.CountryId,
                        addrDto.StateId,
                        cancellationToken
                    );
                    if (!validateResult.IsValid)
                        return new ApiResponse<bool>(false, validateResult.Message, false);

                    companyAddressEntity = new Address
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

                    await _dbContext.Addresses.AddAsync(companyAddressEntity, cancellationToken);
                }

                // Dirección del administrador (separada)
                if (request.CompanyTax.AdminAddress is not null)
                {
                    var addrDto = request.CompanyTax.AdminAddress;
                    var validateResult = await ValidateAddressAsync(
                        addrDto.CountryId,
                        addrDto.StateId,
                        cancellationToken
                    );
                    if (!validateResult.IsValid)
                        return new ApiResponse<bool>(false, validateResult.Message, false);

                    adminAddressEntity = new Address
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

                    await _dbContext.Addresses.AddAsync(adminAddressEntity, cancellationToken);
                }
            }
            // Para INDIVIDUALES: usar Address como dirección compartida
            else
            {
                if (request.CompanyTax.Address is not null)
                {
                    var addrDto = request.CompanyTax.Address;
                    var validateResult = await ValidateAddressAsync(
                        addrDto.CountryId,
                        addrDto.StateId,
                        cancellationToken
                    );
                    if (!validateResult.IsValid)
                        return new ApiResponse<bool>(false, validateResult.Message, false);

                    companyAddressEntity = new Address
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

                    await _dbContext.Addresses.AddAsync(companyAddressEntity, cancellationToken);

                    // Para individuales: misma dirección para usuario
                    adminAddressEntity = companyAddressEntity;
                }
            }

            // Guardar direcciones primero para obtener sus IDs
            if (companyAddressEntity != null)
            {
                var addressesSaved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
                if (!addressesSaved)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to save addresses");
                    return new ApiResponse<bool>(false, "Failed to save addresses", false);
                }
            }

            // Crear CustomPlan con CompanyId desde el inicio
            var customPlan = await CreateCustomPlanAsync(
                companyId,
                serviceLevel,
                serviceConfig.ServiceId,
                serviceConfig.Price,
                serviceConfig.UserLimit,
                cancellationToken
            );
            if (customPlan == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to create CustomPlan for ServiceLevel: {ServiceLevel}",
                    serviceLevel
                );
                return new ApiResponse<bool>(false, "Failed to create custom plan", false);
            }

            // Crear Company
            var company = _mapper.Map<Company>(request.CompanyTax);
            company.Id = companyId;
            company.CustomPlanId = customPlan.Id;
            company.AddressId = companyAddressEntity?.Id;
            company.CreatedAt = DateTime.UtcNow;

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            // Crear TaxUser Owner
            var (token, expiration) = _confirmTokenService.Generate(
                adminUserId,
                request.CompanyTax.Email
            );

            var adminUser = new TaxUser
            {
                Id = adminUserId,
                CompanyId = companyId,
                Email = request.CompanyTax.Email,
                Password = _passwordHash.HashPassword(request.CompanyTax.Password),
                Name = request.CompanyTax.Name,
                LastName = request.CompanyTax.LastName,
                PhoneNumber = request.CompanyTax.PhoneNumber,
                PhotoUrl = request.CompanyTax.PhotoUrl,
                AddressId = adminAddressEntity?.Id,
                IsActive = false,
                IsOwner = true,
                Confirm = false,
                ConfirmToken = token,
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.TaxUsers.AddAsync(adminUser, cancellationToken);

            // Asignar rol Administrator
            await _dbContext.UserRoles.AddAsync(
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = adminUser.Id,
                    RoleId = serviceConfig.AdminRoleId,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken
            );

            // Guardar todas las entidades
            var finalResult = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!finalResult)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create company and user");
                return new ApiResponse<bool>(false, "Failed to create company", false);
            }

            await transaction.CommitAsync(cancellationToken);

            // Construir payloads de address para el evento
            AddressPayload? companyAddressPayload = null;
            AddressPayload? adminAddressPayload = null;

            if (request.CompanyTax.IsCompany)
            {
                // EMPRESAS: CompanyAddress y UserAddress separadas
                companyAddressPayload = await BuildAddressPayloadAsync(
                    companyAddressEntity,
                    cancellationToken
                );
                adminAddressPayload = await BuildAddressPayloadAsync(
                    adminAddressEntity,
                    cancellationToken
                );
            }
            else
            {
                // INDIVIDUALES: Misma dirección compartida
                var sharedAddressPayload = await BuildAddressPayloadAsync(
                    companyAddressEntity,
                    cancellationToken
                );
                companyAddressPayload = sharedAddressPayload;
                adminAddressPayload = sharedAddressPayload;
            }

            // Link de confirmación
            string link = _linkBuilder.BuildConfirmationLink(
                request.Origin,
                adminUser.Email,
                token
            );

            _logger.LogInformation("Company created successfully: {CompanyId}", company.Id);

            // Publicar eventos
            _eventBus.Publish(
                new AccountRegisteredEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    UserId: company.Id,
                    Email: adminUser.Email,
                    Name: adminUser.Name ?? string.Empty,
                    LastName: adminUser.LastName ?? string.Empty,
                    Phone: adminUser.PhoneNumber ?? string.Empty,
                    IsCompany: company.IsCompany,
                    CompanyId: company.Id,
                    FullName: company.FullName,
                    CompanyName: company.CompanyName,
                    Domain: company.Domain,
                    Brand: company.Brand,
                    CompanyAddress: companyAddressPayload,
                    UserAddress: adminAddressPayload
                )
            );

            _eventBus.Publish(
                new AccountConfirmationLinkEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    UserId: adminUser.Id,
                    Email: adminUser.Email,
                    DisplayName: company.IsCompany
                        ? (company.CompanyName ?? company.FullName ?? adminUser.Email)
                        : $"{adminUser.Name} {adminUser.LastName}".Trim(),
                    ConfirmLink: link,
                    ExpiresAt: expiration,
                    CompanyId: company.Id,
                    IsCompany: company.IsCompany,
                    CompanyFullName: company.FullName,
                    CompanyName: company.CompanyName,
                    AdminName: $"{adminUser.Name} {adminUser.LastName}".Trim(),
                    Domain: company.Domain
                )
            );

            return new ApiResponse<bool>(true, "Company created successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating company: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    #region Helper Methods

    private async Task<(bool IsValid, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        // Validar país y estado en una consulta
        var addressValidation = await (
            from c in _dbContext.Countries
            join s in _dbContext.States on c.Id equals s.CountryId
            where c.Id == countryId && s.Id == stateId
            select new
            {
                CountryName = c.Name,
                StateName = s.Name,
                IsUSA = c.Id == 220, // USA country ID - configurable
            }
        ).FirstOrDefaultAsync(ct);

        if (addressValidation == null)
        {
            return (false, $"Invalid CountryId '{countryId}' or StateId '{stateId}'");
        }

        if (!addressValidation.IsUSA)
        {
            return (false, "Only United States addresses are currently supported");
        }

        return (true, "OK");
    }

    private async Task<AddressPayload?> BuildAddressPayloadAsync(
        Address? address,
        CancellationToken ct
    )
    {
        if (address == null)
            return null;

        var geoInfo = await (
            from c in _dbContext.Countries
            join s in _dbContext.States on c.Id equals s.CountryId
            where c.Id == address.CountryId && s.Id == address.StateId
            select new { CountryName = c.Name, StateName = s.Name }
        ).FirstOrDefaultAsync(ct);

        return new AddressPayload(
            CountryId: address.CountryId,
            CountryName: geoInfo?.CountryName ?? string.Empty,
            StateId: address.StateId,
            StateName: geoInfo?.StateName ?? string.Empty,
            City: address.City?.Trim(),
            Street: address.Street?.Trim(),
            Line: address.Line?.Trim(),
            ZipCode: address.ZipCode?.Trim()
        );
    }

    private ServiceLevel DetermineServiceLevel(NewCompanyDTO companyDto)
    {
        if (companyDto.ServiceLevel.HasValue)
        {
            _logger.LogInformation(
                "Using user-specified ServiceLevel: {ServiceLevel}",
                companyDto.ServiceLevel.Value
            );
            return companyDto.ServiceLevel.Value;
        }

        var defaultLevel = companyDto.IsCompany ? ServiceLevel.Standard : ServiceLevel.Basic;
        _logger.LogInformation(
            "{AccountType} registration - defaulting to {ServiceLevel} ServiceLevel",
            companyDto.IsCompany ? "Company" : "Individual",
            defaultLevel
        );
        return defaultLevel;
    }

    private static string GetAdministratorRoleName(ServiceLevel serviceLevel)
    {
        return serviceLevel switch
        {
            ServiceLevel.Basic => "Administrator Basic",
            ServiceLevel.Standard => "Administrator Standard",
            ServiceLevel.Pro => "Administrator Pro",
            _ => "Administrator Basic",
        };
    }

    private async Task<CustomPlan?> CreateCustomPlanAsync(
        Guid companyId,
        ServiceLevel serviceLevel,
        Guid baseServiceId,
        decimal servicePrice,
        int serviceUserLimit, // 🆕 NUEVO PARÁMETRO
        CancellationToken ct
    )
    {
        try
        {
            // 🆕 Crear CustomPlan con UserLimit
            var customPlan = new CustomPlan
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Price = servicePrice,
                UserLimit = serviceUserLimit,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                RenewDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomPlans.AddAsync(customPlan, ct);

            // Resto del método sin cambios...
            var moduleQuery =
                from m in _dbContext.Modules
                where m.ServiceId == baseServiceId && m.IsActive
                select new { m.Id, m.Name };

            var modules = await moduleQuery.ToListAsync(ct);

            foreach (var module in modules)
            {
                var customModule = new CustomModule
                {
                    Id = Guid.NewGuid(),
                    CustomPlanId = customPlan.Id,
                    ModuleId = module.Id,
                    IsIncluded = true,
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.CustomModules.AddAsync(customModule, ct);
                _logger.LogDebug("Added module {ModuleName} to CustomPlan", module.Name);
            }

            var saved = await _dbContext.SaveChangesAsync(ct) > 0;
            if (!saved)
            {
                _logger.LogError("Failed to save CustomPlan and modules");
                return null;
            }

            _logger.LogInformation(
                "CustomPlan created successfully with {ModuleCount} modules for {ServiceLevel}, UserLimit: {UserLimit}",
                modules.Count,
                serviceLevel,
                serviceUserLimit
            );

            return customPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating CustomPlan for ServiceLevel: {ServiceLevel}",
                serviceLevel
            );
            return null;
        }
    }

    #endregion
}
