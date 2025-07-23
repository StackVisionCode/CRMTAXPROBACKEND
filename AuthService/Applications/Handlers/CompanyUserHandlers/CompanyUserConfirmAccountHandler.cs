using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthService.Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace AuthService.Handlers.CompanyUserHandlers;

public class CompanyUserConfirmAccountHandler
    : IRequestHandler<CompanyAccountConfirmCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyUserConfirmAccountHandler> _log;
    private readonly JwtSettings _jwt;
    private readonly IEventBus _eventBus;

    public CompanyUserConfirmAccountHandler(
        ApplicationDbContext db,
        ILogger<CompanyUserConfirmAccountHandler> log,
        IOptions<JwtSettings> jwt,
        IEventBus eventBus
    )
    {
        _db = db;
        _log = log;
        _jwt = jwt.Value;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<Unit>> Handle(
        CompanyAccountConfirmCommands c,
        CancellationToken ct
    )
    {
        // 🏢 JOIN EXPLÍCITO: CompanyUser con Profile y Company
        var companyUserData = await (
            from cu in _db.CompanyUsers
            where cu.Email == c.Email
            join cup in _db.CompanyUserProfiles on cu.Id equals cup.CompanyUserId into prof
            from cup in prof.DefaultIfEmpty() // LEFT JOIN perfil
            join co in _db.Companies on cu.CompanyId equals co.Id into comp
            from co in comp.DefaultIfEmpty() // LEFT JOIN compañía
            select new
            {
                User = cu,
                Profile = cup,
                Company = co,
            }
        ).FirstOrDefaultAsync(ct);

        if (companyUserData is null)
            return new(false, "Cuenta empresarial no encontrada");

        var user = companyUserData.User;

        if (user.Confirm is true)
            return new(false, "La cuenta empresarial ya está confirmada");

        if (user.ConfirmToken != c.Token)
            return new(false, "Token de confirmación inválido");

        // 🔐 VALIDAR EXPIRACIÓN DEL TOKEN JWT
        var handler = new JwtSecurityTokenHandler();
        var tokenValidationParams = new TokenValidationParameters
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
            handler.ValidateToken(c.Token, tokenValidationParams, out _);
        }
        catch (SecurityTokenException)
        {
            return new(false, "Token expirado o inválido");
        }

        // ✅ ACTUALIZAR ESTADO DE CONFIRMACIÓN CON ExecuteUpdateAsync
        var updateResult = await _db
            .CompanyUsers.Where(cu => cu.Id == user.Id)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(cu => cu.Confirm, true)
                        .SetProperty(cu => cu.IsActive, true)
                        .SetProperty(cu => cu.UpdatedAt, DateTime.UtcNow),
                ct
            );

        if (updateResult == 0)
            return new(false, "Error al confirmar la cuenta empresarial");

        // 📧 PUBLICAR EVENTO DE CUENTA EMPRESARIAL ACTIVADA
        string displayName =
            companyUserData.Company?.CompanyName
            ?? $"{companyUserData.Profile?.Name} {companyUserData.Profile?.LastName}".Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = user.Email;

        _eventBus.Publish(
            new CompanyAccountConfirmedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                user.Id,
                user.Email,
                displayName,
                companyUserData.Company?.CompanyName ?? "Empresa",
                companyUserData.Profile?.Name ?? "Usuario",
                companyUserData.Profile?.LastName ?? "",
                companyUserData.Profile?.Position ?? "Miembro del equipo"
            )
        );

        _log.LogInformation(
            "Company user account confirmed: Email={Email}, UserId={UserId}, Company={CompanyName}",
            c.Email,
            user.Id,
            companyUserData.Company?.CompanyName ?? "Unknown"
        );

        return new(true, "Cuenta empresarial confirmada exitosamente");
    }
}
