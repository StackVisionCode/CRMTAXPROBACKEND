using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthService.Commands.UserConfirmCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace AuthService.Handlers.UserTaxHandlers;

public class ConfirmAccountHandler : IRequestHandler<AccountConfirmCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ConfirmAccountHandler> _log;
    private readonly JwtSettings _jwt;
    private readonly IEventBus _eventBus;

    public ConfirmAccountHandler(
        ApplicationDbContext db,
        ILogger<ConfirmAccountHandler> log,
        IOptions<JwtSettings> jwt,
        IEventBus eventBus
    )
    {
        _db = db;
        _log = log;
        _jwt = jwt.Value;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<Unit>> Handle(AccountConfirmCommands c, CancellationToken ct)
    {
        var data = await (
            from u in _db.TaxUsers
            where u.Email == c.Email
            join co in _db.Companies on u.CompanyId equals co.Id into comp
            from co in comp.DefaultIfEmpty()
            select new
            {
                User = u,
                Company = co,
                // Obtener roles del usuario
                UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            }
        ).FirstOrDefaultAsync(ct);

        if (data is null)
            return new(false, "Account not found");

        var user = data.User;

        if (user.Confirm is true)
            return new(false, "Account is already confirmed");

        if (user.ConfirmToken != c.Token)
            return new(false, "Invalid token");

        // Validar expiración del token
        var handler = new JwtSecurityTokenHandler();
        var prm = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            handler.ValidateToken(c.Token, prm, out _);
        }
        catch (SecurityTokenException)
        {
            return new(false, "Token expired or invalid");
        }

        user.Confirm = true;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        bool isDeveloper = data.UserRoles.Contains("Developer");
        bool isAdministrator = data.UserRoles.Any(r => r.Contains("Administrator"));

        string fullNameForDisplay = BuildFullNameForDisplay(data);

        _eventBus.Publish(
            new AccountConfirmedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                data.Company?.Id ?? Guid.Empty,
                data.User?.Name,
                data.User?.LastName,
                fullNameForDisplay,
                data.Company?.CompanyName,
                data.Company?.Domain,
                data.Company?.IsCompany ?? false,
                user.Id,
                user.Email
            )
        );

        if (isDeveloper)
        {
            _log.LogInformation("Developer account {Email} confirmed", c.Email);
        }
        else if (isAdministrator)
        {
            _log.LogInformation(
                "Company Administrator account {Email} confirmed for company {CompanyId}",
                c.Email,
                data.Company?.Id
            );
        }
        else
        {
            _log.LogWarning(
                "Unexpected role confirmation for {Email} with roles: {Roles}",
                c.Email,
                string.Join(", ", data.UserRoles)
            );
        }

        return new(true, "Account confirmed");
    }

    /// <summary>
    /// Construye el FullName correcto dependiendo del tipo de cuenta
    /// </summary>
    private static string BuildFullNameForDisplay(dynamic data)
    {
        // Para empresas: usar CompanyName
        if (
            data.Company?.IsCompany == true
            && !string.IsNullOrWhiteSpace(data.Company?.CompanyName)
        )
        {
            return data.Company.CompanyName;
        }

        // Para individuales: construir nombre completo del usuario
        if (data.Company?.IsCompany == false)
        {
            // Opción 1: Si la company tiene FullName (nombre del preparador individual)
            if (!string.IsNullOrWhiteSpace(data.Company?.FullName))
            {
                return data.Company.FullName;
            }

            // Opción 2: Construir desde Name + LastName del usuario
            var name = data.User?.Name?.Trim();
            var lastName = data.User?.LastName?.Trim();

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(lastName))
            {
                return $"{name} {lastName}";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name ?? "User";
            }
        }

        // Fallback: usar email del usuario
        return data.User?.Email ?? "Unknown User";
    }
}
