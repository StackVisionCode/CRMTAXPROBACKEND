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
        var user = await _db.TaxUsers.FirstOrDefaultAsync(u => u.Email == c.Email, ct);
        if (user is null)
            return new(false, "Cuenta no encontrada");

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

        // ► Evento de cuenta activada
        string display = user.TaxUserProfile?.Name is { Length: > 0 }
            ? $"{user.TaxUserProfile.Name} {user.TaxUserProfile.LastName}".Trim()
            : user.Company?.CompanyName ?? user.Email;

        _eventBus.Publish(
            new AccountConfirmedEvent(Guid.NewGuid(), DateTime.UtcNow, user.Id, user.Email, display)
        );

        _log.LogInformation("Cuenta {Email} confirmada", c.Email);
        return new(true, "Cuenta confirmada");
    }
}
