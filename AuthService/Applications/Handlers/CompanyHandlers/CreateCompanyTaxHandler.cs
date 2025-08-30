using Applications.Common;
using Applications.DTOs.AddressDTOs;
using AuthService.Applications.Common;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Addresses;
using AuthService.Domains.Companies;
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
            // 1. VALIDACIONES INICIALES
            var validationResult = await ValidateCompanyCreationAsync(
                request.CompanyTax,
                cancellationToken
            );
            if (!validationResult.IsValid)
            {
                return new ApiResponse<bool>(false, validationResult.Message, false);
            }

            // 2. DETERMINAR SERVICELEVEL Y VALIDAR ROL
            var serviceLevel = DetermineServiceLevel(request.CompanyTax);
            var adminRoleName = GetAdministratorRoleName(serviceLevel);

            var adminRole = await _dbContext
                .Roles.Where(r =>
                    r.Name == adminRoleName
                    && (r.ServiceLevel == serviceLevel || r.ServiceLevel == null)
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (adminRole == null)
            {
                _logger.LogError(
                    "Administrator role not found: {RoleName} for ServiceLevel: {ServiceLevel}",
                    adminRoleName,
                    serviceLevel
                );
                return new ApiResponse<bool>(
                    false,
                    $"Administrator role configuration not found for {serviceLevel} level",
                    false
                );
            }

            _logger.LogInformation(
                "Creating company with ServiceLevel: {ServiceLevel}, Role: {RoleName}",
                serviceLevel,
                adminRoleName
            );

            // 3. GENERAR IDs ÚNICOS
            var companyId = Guid.NewGuid();
            var adminUserId = Guid.NewGuid();

            // 4. CREAR DIRECCIONES
            var (companyAddressEntity, adminAddressEntity) = await CreateAddressesAsync(
                request.CompanyTax,
                cancellationToken
            );

            // 5. CREAR COMPANY
            var company = _mapper.Map<Company>(request.CompanyTax);
            company.Id = companyId;
            company.ServiceLevel = serviceLevel; // NUEVO - solo almacenar ServiceLevel
            company.AddressId = companyAddressEntity?.Id;
            company.CreatedAt = DateTime.UtcNow;

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            // 6. CREAR TAXUSER OWNER
            var adminUser = CreateAdminUser(
                request.CompanyTax,
                adminUserId,
                companyId,
                adminAddressEntity
            );

            await _dbContext.TaxUsers.AddAsync(adminUser, cancellationToken);

            // 7. ASIGNAR ROL ADMINISTRATOR
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

            // 8. GUARDAR TODAS LAS ENTIDADES
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create company and user");
                return new ApiResponse<bool>(false, "Failed to create company", false);
            }

            await transaction.CommitAsync(cancellationToken);

            // 9. PUBLICAR EVENTOS (mantener eventos existentes para otros microservicios)
            await PublishEventsAsync(
                company,
                adminUser,
                companyAddressEntity,
                adminAddressEntity,
                request.Origin,
                cancellationToken
            );

            _logger.LogInformation(
                "Company created successfully: {CompanyId} with ServiceLevel: {ServiceLevel}",
                company.Id,
                serviceLevel
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

    #region Validation Methods

    private async Task<(bool IsValid, string Message)> ValidateCompanyCreationAsync(
        NewCompanyDTO companyDto,
        CancellationToken cancellationToken
    )
    {
        // Verificar email único
        var emailExists = await _dbContext.TaxUsers.AnyAsync(
            u => u.Email == companyDto.Email,
            cancellationToken
        );

        if (emailExists)
        {
            _logger.LogWarning("Email already exists: {Email}", companyDto.Email);
            return (false, "Email already exists");
        }

        // Verificar domain único
        var domainExists = await _dbContext.Companies.AnyAsync(
            c => c.Domain == companyDto.Domain,
            cancellationToken
        );

        if (domainExists)
        {
            _logger.LogWarning("Domain already exists: {Domain}", companyDto.Domain);
            return (false, "Domain already exists");
        }

        // Validar ServiceLevel
        if (!Enum.IsDefined(typeof(ServiceLevel), companyDto.ServiceLevel))
        {
            _logger.LogWarning(
                "Invalid ServiceLevel specified: {ServiceLevel}",
                companyDto.ServiceLevel
            );
            return (
                false,
                "Invalid ServiceLevel. Valid values: 1=Basic, 2=Standard, 3=Pro, 99=Developer"
            );
        }

        return (true, "Valid");
    }

    private async Task<(bool IsValid, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken cancellationToken
    )
    {
        var addressValidation = await (
            from c in _dbContext.Countries
            join s in _dbContext.States on c.Id equals s.CountryId
            where c.Id == countryId && s.Id == stateId
            select new
            {
                CountryName = c.Name,
                StateName = s.Name,
                IsUSA = c.Id == 220,
            }
        ).FirstOrDefaultAsync(cancellationToken);

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

    #endregion

    #region Creation Methods

    private async Task<(Address? companyAddress, Address? adminAddress)> CreateAddressesAsync(
        NewCompanyDTO companyDto,
        CancellationToken cancellationToken
    )
    {
        Address? companyAddressEntity = null;
        Address? adminAddressEntity = null;

        if (companyDto.IsCompany)
        {
            // EMPRESAS: crear direcciones separadas
            if (companyDto.Address != null)
            {
                var validation = await ValidateAddressAsync(
                    companyDto.Address.CountryId,
                    companyDto.Address.StateId,
                    cancellationToken
                );
                if (!validation.IsValid)
                    throw new InvalidOperationException(
                        $"Invalid company address: {validation.Message}"
                    );

                companyAddressEntity = CreateAddressEntity(companyDto.Address);
                await _dbContext.Addresses.AddAsync(companyAddressEntity, cancellationToken);
            }

            if (companyDto.AdminAddress != null)
            {
                var validation = await ValidateAddressAsync(
                    companyDto.AdminAddress.CountryId,
                    companyDto.AdminAddress.StateId,
                    cancellationToken
                );
                if (!validation.IsValid)
                    throw new InvalidOperationException(
                        $"Invalid admin address: {validation.Message}"
                    );

                adminAddressEntity = CreateAddressEntity(companyDto.AdminAddress);
                await _dbContext.Addresses.AddAsync(adminAddressEntity, cancellationToken);
            }
        }
        else
        {
            // INDIVIDUALES: dirección compartida
            if (companyDto.Address != null)
            {
                var validation = await ValidateAddressAsync(
                    companyDto.Address.CountryId,
                    companyDto.Address.StateId,
                    cancellationToken
                );
                if (!validation.IsValid)
                    throw new InvalidOperationException($"Invalid address: {validation.Message}");

                companyAddressEntity = CreateAddressEntity(companyDto.Address);
                await _dbContext.Addresses.AddAsync(companyAddressEntity, cancellationToken);
                adminAddressEntity = companyAddressEntity; // Misma dirección
            }
        }

        // Guardar direcciones si existen
        if (companyAddressEntity != null)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (companyAddressEntity, adminAddressEntity);
    }

    private static Address CreateAddressEntity(AddressDTO addressDto)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            CountryId = addressDto.CountryId,
            StateId = addressDto.StateId,
            City = addressDto.City?.Trim(),
            Street = addressDto.Street?.Trim(),
            Line = addressDto.Line?.Trim(),
            ZipCode = addressDto.ZipCode?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    private TaxUser CreateAdminUser(
        NewCompanyDTO companyDto,
        Guid adminUserId,
        Guid companyId,
        Address? adminAddress
    )
    {
        var (token, expiration) = _confirmTokenService.Generate(adminUserId, companyDto.Email);

        return new TaxUser
        {
            Id = adminUserId,
            CompanyId = companyId,
            Email = companyDto.Email,
            Password = _passwordHash.HashPassword(companyDto.Password),
            Name = companyDto.Name,
            LastName = companyDto.LastName,
            PhoneNumber = companyDto.PhoneNumber,
            PhotoUrl = companyDto.PhotoUrl,
            AddressId = adminAddress?.Id,
            IsActive = false,
            IsOwner = true,
            Confirm = false,
            ConfirmToken = token,
            OtpVerified = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    #endregion

    #region Helper Methods

    private ServiceLevel DetermineServiceLevel(NewCompanyDTO companyDto)
    {
        // Usar el ServiceLevel especificado o determinar por defecto
        var serviceLevel = companyDto.ServiceLevel;

        _logger.LogInformation(
            "Using ServiceLevel: {ServiceLevel} for {AccountType} registration",
            serviceLevel,
            companyDto.IsCompany ? "Company" : "Individual"
        );

        return serviceLevel;
    }

    private static string GetAdministratorRoleName(ServiceLevel serviceLevel)
    {
        return serviceLevel switch
        {
            ServiceLevel.Basic => "Administrator Basic",
            ServiceLevel.Standard => "Administrator Standard",
            ServiceLevel.Pro => "Administrator Pro",
            ServiceLevel.Developer => "Developer",
            _ => "Administrator Basic",
        };
    }

    private async Task PublishEventsAsync(
        Company company,
        TaxUser adminUser,
        Address? companyAddress,
        Address? adminAddress,
        string origin,
        CancellationToken cancellationToken
    )
    {
        // Construir payloads de direcciones
        var companyAddressPayload = await BuildAddressPayloadAsync(
            companyAddress,
            cancellationToken
        );
        var adminAddressPayload = await BuildAddressPayloadAsync(adminAddress, cancellationToken);

        // Link de confirmación
        string confirmationLink = _linkBuilder.BuildConfirmationLink(
            origin,
            adminUser.Email,
            adminUser.ConfirmToken!
        );

        // Evento de registro (para otros microservicios)
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

        // Evento de confirmación (para email service)
        _eventBus.Publish(
            new AccountConfirmationLinkEvent(
                Id: Guid.NewGuid(),
                OccurredOn: DateTime.UtcNow,
                UserId: adminUser.Id,
                Email: adminUser.Email,
                DisplayName: company.IsCompany
                    ? (company.CompanyName ?? company.FullName ?? adminUser.Email)
                    : $"{adminUser.Name} {adminUser.LastName}".Trim(),
                ConfirmLink: confirmationLink,
                ExpiresAt: DateTime.UtcNow.AddHours(24), // Configurar según necesidades
                CompanyId: company.Id,
                IsCompany: company.IsCompany,
                CompanyFullName: company.FullName,
                CompanyName: company.CompanyName,
                AdminName: $"{adminUser.Name} {adminUser.LastName}".Trim(),
                Domain: company.Domain
            )
        );
    }

    private async Task<AddressPayload?> BuildAddressPayloadAsync(
        Address? address,
        CancellationToken cancellationToken
    )
    {
        if (address == null)
            return null;

        var geoInfo = await (
            from c in _dbContext.Countries
            join s in _dbContext.States on c.Id equals s.CountryId
            where c.Id == address.CountryId && s.Id == address.StateId
            select new { CountryName = c.Name, StateName = s.Name }
        ).FirstOrDefaultAsync(cancellationToken);

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

    #endregion
}
