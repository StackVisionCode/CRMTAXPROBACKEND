using AuthService.Commands.ResetPasswordCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common.Helpers;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
using SharedLibrary.Services;

namespace AuthService.Handlers.ResetPasswordHandlers;

public class RequestPasswordResetHandler
    : IRequestHandler<RequestPasswordResetCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly IResetTokenService _tokenSrv;
    private readonly IEventBus _bus;
    private readonly ILogger<RequestPasswordResetHandler> _log;

    public RequestPasswordResetHandler(
        ApplicationDbContext db,
        IResetTokenService tokenSrv,
        IEventBus bus,
        ILogger<RequestPasswordResetHandler> log
    )
    {
        _db = db;
        _tokenSrv = tokenSrv;
        _bus = bus;
        _log = log;
    }

    public async Task<ApiResponse<Unit>> Handle(
        RequestPasswordResetCommands r,
        CancellationToken ct
    )
    {
        try
        {
            // 🔍 BUSCAR USUARIO CON COMPANY INFO
            var userData = await (
                from u in _db.TaxUsers
                where u.Email == r.Email && u.IsActive
                join c in _db.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    // Datos del usuario (ahora en TaxUser directamente)
                    Name = u.Name,
                    LastName = u.LastName,
                    // Datos de la company
                    CompanyFullName = c != null ? c.FullName : null,
                    CompanyName = c != null ? c.CompanyName : null,
                    IsCompany = c != null ? c.IsCompany : false,
                }
            ).FirstOrDefaultAsync(ct);

            if (userData == null || !userData.IsActive)
            {
                _log.LogWarning(
                    "Password reset requested for non-existent or inactive user: {Email}",
                    r.Email
                );
                return new(false, "Cuenta no encontrada o inactiva");
            }

            // 🔑 GENERAR TOKEN DE RESET
            var (token, exp) = _tokenSrv.Generate(userData.UserId, userData.Email);

            // ACTUALIZAR USUARIO CON TOKEN
            var updateResult =
                await _db
                    .TaxUsers.Where(u => u.Id == userData.UserId)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(u => u.ResetPasswordToken, token)
                                .SetProperty(u => u.ResetPasswordExpires, exp)
                                .SetProperty(u => u.Otp, (string?)null) // invalidar OTP previa
                                .SetProperty(u => u.OtpExpires, (DateTime?)null)
                                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                        ct
                    ) > 0;

            if (!updateResult)
            {
                _log.LogError("Failed to update reset token for user: {Email}", userData.Email);
                return new(false, "Error al procesar la solicitud de reset");
            }

            // 🔗 GENERAR LINK DE RESET
            var link =
                $"{r.Origin.TrimEnd('/')}/auth/reset-password/validate?email={Uri.EscapeDataString(userData.Email)}&token={Uri.EscapeDataString(token)}";

            // 📝 CALCULAR DISPLAY NAME CON NUEVA ESTRUCTURA
            var display = DisplayNameHelper.From(
                userData.Name,
                userData.LastName,
                userData.CompanyFullName,
                userData.CompanyName,
                userData.IsCompany,
                userData.Email
            );

            // 📧 PUBLICAR EVENTO DE RESET
            _bus.Publish(
                new PasswordResetLinkEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userData.UserId,
                    userData.Email,
                    display,
                    link,
                    exp
                )
            );

            _log.LogInformation(
                "Password reset link sent: Email={Email}, UserId={UserId}",
                userData.Email,
                userData.UserId
            );

            return new(true, "Se envió un correo con el enlace para reestablecer contraseña");
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error al procesar solicitud de reseteo de contraseña para {Email}",
                r.Email
            );
            return new(false, "Error al procesar la solicitud, intente más tarde");
        }
    }
}
