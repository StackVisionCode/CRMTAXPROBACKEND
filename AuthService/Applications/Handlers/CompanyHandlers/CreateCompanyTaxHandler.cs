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

    // Constantes de geografía (US-only)
    private const int USA = 220;

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

                _logger.LogInformation("Validated ServiceLevel: {ServiceLevel}", requestedLevel);
            }

            // Obtener rol Administrator usando Join
            var serviceLevel = DetermineServiceLevel(request.CompanyTax);
            var adminRoleName = GetAdministratorRoleName(serviceLevel);

            var adminRoleQuery =
                from r in _dbContext.Roles
                where r.Name == adminRoleName
                select new
                {
                    r.Id,
                    r.Name,
                    r.ServiceLevel,
                };

            var adminRole = await adminRoleQuery.FirstOrDefaultAsync(cancellationToken);
            if (adminRole == null)
            {
                _logger.LogError(
                    "Administrator role not found for ServiceLevel: {ServiceLevel}",
                    serviceLevel
                );
                return new ApiResponse<bool>(
                    false,
                    $"Administrator role not found for {serviceLevel}",
                    false
                );
            }

            _logger.LogInformation(
                "Assigning role: {RoleName} for ServiceLevel: {ServiceLevel}",
                adminRole.Name,
                serviceLevel
            );

            // Lógica de direcciones según el tipo de cuenta
            Address? companyAddressEntity = null;
            Address? adminAddressEntity = null;

            // 3a. Para EMPRESAS: crear ambas direcciones si existen
            if (request.CompanyTax.IsCompany)
            {
                // Dirección de la empresa (usando Address del DTO)
                if (request.CompanyTax.Address is not null)
                {
                    var addrDto = request.CompanyTax.Address;
                    var validateResult = await ValidateAddressAsync(
                        addrDto.CountryId,
                        addrDto.StateId,
                        cancellationToken
                    );
                    if (!validateResult.Success)
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
                    _logger.LogDebug(
                        "Created company address with ID: {AddressId}",
                        companyAddressEntity.Id
                    );
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
                    if (!validateResult.Success)
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
                    _logger.LogDebug(
                        "Created admin address with ID: {AddressId}",
                        adminAddressEntity.Id
                    );
                }
            }
            // 3b. Para INDIVIDUALES: usar Address como dirección del usuario
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
                    if (!validateResult.Success)
                        return new ApiResponse<bool>(false, validateResult.Message, false);

                    // Para individuales: Address se usa tanto para company como para user
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

                    _logger.LogDebug(
                        "Created individual address with ID: {AddressId} (shared for company and user)",
                        companyAddressEntity.Id
                    );
                }
            }

            // 4. GUARDAR DIRECCIONES PRIMERO para obtener sus IDs
            if (companyAddressEntity != null)
            {
                var addressesSaved = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
                if (!addressesSaved)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to save addresses");
                    return new ApiResponse<bool>(false, "Failed to save addresses", false);
                }
                _logger.LogDebug("Addresses saved successfully");
            }

            // Crear CustomPlan
            var customPlan = await CreateCustomPlanAsync(serviceLevel, cancellationToken);
            if (customPlan == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to create CustomPlan for ServiceLevel: {ServiceLevel}",
                    serviceLevel
                );
                return new ApiResponse<bool>(false, "Failed to create custom plan", false);
            }

            _logger.LogDebug(
                "Created CustomPlan with ID: {CustomPlanId} for ServiceLevel: {ServiceLevel}",
                customPlan.Id,
                serviceLevel
            );

            // 5. Crear Company
            var company = _mapper.Map<Company>(request.CompanyTax);
            company.Id = Guid.NewGuid();
            company.CustomPlanId = customPlan.Id;
            company.CreatedAt = DateTime.UtcNow;

            // CRÍTICO: Asignar AddressId si existe la dirección
            if (companyAddressEntity != null)
            {
                company.AddressId = companyAddressEntity.Id;
                _logger.LogDebug("Assigned company AddressId: {AddressId}", company.AddressId);
            }

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            // 6. Crear TaxUser para el admin
            var adminUser = new TaxUser
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Email = request.CompanyTax.Email,
                Password = _passwordHash.HashPassword(request.CompanyTax.Password),
                Name = request.CompanyTax.Name,
                LastName = request.CompanyTax.LastName,
                PhoneNumber = request.CompanyTax.PhoneNumber,
                PhotoUrl = request.CompanyTax.PhotoUrl,
                IsActive = false,
                Confirm = false,
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            // CRÍTICO: Asignar AddressId si existe la dirección
            if (adminAddressEntity != null)
            {
                adminUser.AddressId = adminAddressEntity.Id;
                _logger.LogDebug("Assigned admin AddressId: {AddressId}", adminUser.AddressId);
            }

            // 7. Generar token de confirmación
            var (token, expiration) = _confirmTokenService.Generate(adminUser.Id, adminUser.Email);
            adminUser.ConfirmToken = token;

            await _dbContext.TaxUsers.AddAsync(adminUser, cancellationToken);

            // Actualizar CustomPlan con el CompanyId real
            customPlan.CompanyId = company.Id;
            _dbContext.CustomPlans.Update(customPlan);

            // 8. Asignar rol Administrator
            await _dbContext.UserRoles.AddAsync(
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken
            );

            // 9. Guardar Company, User y UserRole
            var finalResult = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (!finalResult)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create company and user");
                return new ApiResponse<bool>(false, "Failed to create company", false);
            }

            await transaction.CommitAsync(cancellationToken);

            AddressPayload? companyAddressPayload = null;
            AddressPayload? adminAddressPayload = null;

            // 10. Construir payloads de address para el evento
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

                _logger.LogDebug(
                    "Company account - CompanyAddress and UserAddress both populated for {Email}",
                    request.CompanyTax.Email
                );
            }
            else
            {
                // INDIVIDUALES: Misma dirección para CompanyAddress y UserAddress
                // porque en individuales, address representa tanto la ubicación del "negocio" como del usuario
                var sharedAddressPayload = await BuildAddressPayloadAsync(
                    companyAddressEntity,
                    cancellationToken
                );
                companyAddressPayload = sharedAddressPayload;
                adminAddressPayload = sharedAddressPayload; // Misma dirección compartida

                _logger.LogDebug(
                    "Individual account - Same address used for both CompanyAddress and UserAddress for {Email}",
                    request.CompanyTax.Email
                );
            }

            // 11. Link de confirmación
            string link = _linkBuilder.BuildConfirmationLink(
                request.Origin,
                adminUser.Email,
                token
            );

            _logger.LogInformation("Company created successfully: {CompanyId}", company.Id);

            // Publicar eventos
            // Evento legacy (sin Address)
            _eventBus.Publish(
                new AccountRegisteredEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    UserId: adminUser.Id,
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

            // Publicacion de evento de correo de confirmacion
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

    // ==========================
    // Helpers privados del handler
    // ==========================

    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        // País (US-only)
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

    /// <summary>
    /// Determina el ServiceLevel basado en el tipo de registro
    /// Por ahora: Basic por defecto, pero se puede personalizar
    /// </summary>
    private ServiceLevel DetermineServiceLevel(NewCompanyDTO companyDto)
    {
        // Si se especifica ServiceLevel explícitamente, usarlo
        if (companyDto.ServiceLevel.HasValue)
        {
            var requestedLevel = companyDto.ServiceLevel.Value;
            _logger.LogInformation(
                "Using user-specified ServiceLevel: {ServiceLevel}",
                requestedLevel
            );
            return requestedLevel;
        }

        // Lógica de negocio por defecto cuando no se especifica
        if (companyDto.IsCompany)
        {
            _logger.LogInformation("Company registration - defaulting to Standard ServiceLevel");
            return ServiceLevel.Standard; // Empresas → Standard por defecto
        }
        else
        {
            _logger.LogInformation("Individual registration - defaulting to Basic ServiceLevel");
            return ServiceLevel.Basic; // Individuales → Basic por defecto
        }
    }

    /// <summary>
    /// Obtiene el nombre del rol Administrator según el ServiceLevel
    /// </summary>
    private string GetAdministratorRoleName(ServiceLevel serviceLevel)
    {
        return serviceLevel switch
        {
            ServiceLevel.Basic => "Administrator Basic",
            ServiceLevel.Standard => "Administrator Standard",
            ServiceLevel.Pro => "Administrator Pro",
            _ => "Administrator Basic", // Fallback
        };
    }

    /// <summary>
    /// Crea un CustomPlan con módulos por defecto según el ServiceLevel
    /// </summary>
    private async Task<CustomPlan?> CreateCustomPlanAsync(
        ServiceLevel serviceLevel,
        CancellationToken ct
    )
    {
        try
        {
            // Obtener Service según ServiceLevel
            var serviceQuery =
                from s in _dbContext.Services
                where s.Name == serviceLevel.ToString() && s.IsActive
                select new
                {
                    s.Id,
                    s.Name,
                    s.Price,
                };

            var service = await serviceQuery.FirstOrDefaultAsync(ct);
            if (service == null)
            {
                _logger.LogError(
                    "Service not found for ServiceLevel: {ServiceLevel}",
                    serviceLevel
                );
                return null;
            }

            // Crear CustomPlan
            var customPlan = new CustomPlan
            {
                Id = Guid.NewGuid(),
                CompanyId = Guid.Empty, // Se asignará después cuando se cree la Company
                Price = service.Price,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = null, // Sin expiración por defecto
                RenewDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomPlans.AddAsync(customPlan, ct);

            // Obtener módulos del Service para crear CustomModules
            var moduleQuery =
                from m in _dbContext.Modules
                where m.ServiceId == service.Id && m.IsActive
                select new { m.Id, m.Name };

            var modules = await moduleQuery.ToListAsync(ct);

            // Crear CustomModules para cada módulo del Service
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

            // Guardar CustomPlan y CustomModules
            var saved = await _dbContext.SaveChangesAsync(ct) > 0;
            if (!saved)
            {
                _logger.LogError("Failed to save CustomPlan and modules");
                return null;
            }

            _logger.LogInformation(
                "CustomPlan created successfully with {ModuleCount} modules for {ServiceLevel}",
                modules.Count,
                serviceLevel
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
}
