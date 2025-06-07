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
            var user = await _db
                .TaxUsers.Include(u => u.TaxUserProfile)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == r.Email, ct);

            if (user is null || !user.IsActive)
                return new(false, "Cuenta no encontrada o inactiva");

            // 1. Generar token y link
            var (token, exp) = _tokenSrv.Generate(user.Id, user.Email);
            user.ResetPasswordToken = token;
            user.ResetPasswordExpires = exp;
            user.Otp = null; // invalidar cualquier OTP previa
            user.OtpExpires = null;

            await _db.SaveChangesAsync(ct);

            // 2. Publicar evento a EmailService
            var link = $"{r.Origin.TrimEnd('/')}/auth/reset-password?token={token}";
            var display = DisplayNameHelper.From(
                user.TaxUserProfile?.Name,
                user.TaxUserProfile?.LastName,
                user.Company?.CompanyName,
                user.Email
            );

            _bus.Publish(
                new PasswordResetLinkEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    user.Id,
                    user.Email,
                    display,
                    link,
                    exp
                )
            );

            _log.LogInformation("Link de reseteo enviado a {Email}", user.Email);
            return new(true, "Se envió un correo con el enlace para reestablecer contraseña");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al procesar solicitud de reseteo de contraseña");
            return new(false, "Error al procesar la solicitud, intente más tarde");
        }
    }
}
