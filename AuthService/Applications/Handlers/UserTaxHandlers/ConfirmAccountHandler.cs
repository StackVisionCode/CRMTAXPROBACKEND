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
            join p in _db.TaxUserProfiles on u.Id equals p.TaxUserId into prof
            from p in prof.DefaultIfEmpty() // ← LEFT JOIN perfil
            join co in _db.Companies on u.CompanyId equals co.Id into comp
            from co in comp.DefaultIfEmpty() // ← LEFT JOIN compañía
            select new
            {
                User = u,
                Profile = p,
                Company = co,
            }
        ).FirstOrDefaultAsync(ct);

        if (data is null)
            return new(false, "Cuenta no encontrada");

        var user = data.User;

        if (user.Confirm is true)
            return new(false, "La cuenta ya está confirmada");

        if (user.ConfirmToken != c.Token)
            return new(false, "Token inválido");

        // validar expiración
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
            return new(false, "Token expirado o inválido");
        }

        user.Confirm = true;
        user.IsActive = true;
        // user.ConfirmToken = null;
        await _db.SaveChangesAsync(ct);

        bool isCompany = data.Company is not null;

        // ► Evento de cuenta activada
        string display = isCompany
            ? data.Company!.CompanyName ?? user.Email
            : $"{data.Profile?.Name} {data.Profile?.LastName}".Trim();

        _eventBus.Publish(
            new AccountConfirmedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                user.Id,
                user.Email,
                display,
                isCompany
            )
        );

        _log.LogInformation("Cuenta {Email} confirmada", c.Email);
        return new(true, "Cuenta confirmada");
    }
}
