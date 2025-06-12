using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Companies;
using AuthService.Domains.Roles;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.UserTaxHandlers;

public class CreateCompanyTaxHandler : IRequestHandler<CreateTaxCompanyCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateCompanyTaxHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _eventBus;
    private readonly IConfirmTokenService _confirmTokenService;

    public CreateCompanyTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateCompanyTaxHandler> logger,
        IMapper mapper,
        IPasswordHash passwordHash,
        IEventBus eventBus,
        IConfirmTokenService confirmTokenService
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
        _passwordHash = passwordHash;
        _eventBus = eventBus;
        _confirmTokenService = confirmTokenService;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateTaxCompanyCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userExists = await Exists(request.Companytax);
            if (userExists)
            {
                _logger.LogWarning("Company already exists: {Email}", request.Companytax.Email);
                return new ApiResponse<bool>(false, "Company already exists", false);
            }

            request.Companytax.Id = Guid.NewGuid();
            request.Companytax.Password = _passwordHash.HashPassword(request.Companytax.Password!);

            var companyTax = _mapper.Map<Company>(request.Companytax);
            companyTax.CreatedAt = DateTime.UtcNow;

            var UserCompanyTax = _mapper.Map<NewUserDTO>(request.Companytax);
            var MapToUser = _mapper.Map<TaxUser>(UserCompanyTax);
            MapToUser.CompanyId = companyTax.Id;
            MapToUser.IsActive = false;
            MapToUser.Confirm = false;
            MapToUser.CreatedAt = DateTime.UtcNow;
            var RoleGuid = await GetAllRoles();
            MapToUser.RoleId = RoleGuid?.Id ?? Guid.Empty;

            await _dbContext.Companies.AddAsync(companyTax);
            var CompanyResult = await _dbContext.SaveChangesAsync() > 0;
            if (!CompanyResult)
            {
                _logger.LogError("Error creating company tax");
                return new ApiResponse<bool>(false, "Error creating company tax", false);
            }

            var (token, expiration) = _confirmTokenService.Generate(MapToUser.Id, MapToUser.Email);
            MapToUser.ConfirmToken = token;

            await _dbContext.TaxUsers.AddAsync(MapToUser);
            var ResultUserSaved = await _dbContext.SaveChangesAsync() > 0;

            var link =
                $"{request.Origin.TrimEnd('/')}/auth/confirm"
                + $"?email={Uri.EscapeDataString(MapToUser.Email)}"
                + $"&token={Uri.EscapeDataString(token)}";

            if (!ResultUserSaved)
            {
                _logger.LogError("Error creating user tax");
                return new ApiResponse<bool>(false, "Error creating user tax", false);
            }

            _logger.LogInformation("User tax created successfully: {UserTax}", companyTax);

            _eventBus.Publish(
                new AccountConfirmationLinkEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    MapToUser.Id,
                    MapToUser.Email,
                    request.Companytax.FullName
                        ?? request.Companytax.CompanyName
                        ?? MapToUser.Email,
                    link,
                    expiration
                )
            );

            _eventBus.Publish(
                new AccountRegisteredEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    MapToUser.Id,
                    MapToUser.Email,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    true, // IsCompany
                    companyTax.Id,
                    request.Companytax.FullName,
                    request.Companytax.CompanyName,
                    request.Companytax.Domain
                )
            );

            return new ApiResponse<bool>(
                ResultUserSaved,
                ResultUserSaved
                    ? "Company tax created successfully"
                    : "Failed to create company tax",
                ResultUserSaved
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company tax: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    private async Task<bool> Exists(NewCompanyDTO companyDTO)
    {
        try
        {
            return await _dbContext.TaxUsers.FirstOrDefaultAsync(a => a.Email == companyDTO.Email)
                != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if company exists.");
            throw new Exception("Error occurred while checking if company exists.");
        }
    }

    private async Task<Role> GetAllRoles()
    {
        var result = await _dbContext
            .Roles.AsNoTracking()
            .Where(a => a.Name.Contains("administrator"))
            .FirstAsync();
        if (result is null)
        {
            return null!;
        }
        return result;
    }
}
