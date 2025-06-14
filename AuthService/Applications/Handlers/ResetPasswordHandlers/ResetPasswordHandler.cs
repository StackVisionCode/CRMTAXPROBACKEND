using AuthService.Commands.ResetPasswordCommands;
using AuthService.Infraestructure.Services;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Common.Helpers;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace AuthService.Handlers.ResetPasswordHandlers;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _bus; // NUEVO
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        ApplicationDbContext db,
        IPasswordHash passwordHash,
        IEventBus bus, // NUEVO
        ILogger<ResetPasswordHandler> logger
    )
    {
        _db = db;
        _passwordHash = passwordHash;
        _bus = bus;
        _logger = logger;
    }

    public async Task<ApiResponse<Unit>> Handle(ResetPasswordCommands r, CancellationToken ct)
    {
        try
        {
            var user = await _db
                .TaxUsers.Include(u => u.TaxUserProfile)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == r.Email, ct);

            if (user is null)
                return new(false, "Usuario no encontrado");

            if (user.OtpVerified == false)
                return new(false, "OTP no verificada aún");

            // Token aún debe coincidir y seguir vigente
            if (user.ResetPasswordToken != r.Token || user.ResetPasswordExpires < DateTime.UtcNow)
                return new(false, "Token expirado o inválido");

            // 1. Actualizar contraseña y limpiar artefactos
            user.Password = _passwordHash.HashPassword(r.NewPassword);
            // user.ResetPasswordToken = null;
            // user.ResetPasswordExpires = null;
            user.OtpVerified = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            // 2. Calcular DisplayName coherente
            var display = DisplayNameHelper.From(
                user.TaxUserProfile?.Name,
                user.TaxUserProfile?.LastName,
                user.Company?.CompanyName,
                user.Email
            );

            // 3. Publicar evento de confirmación
            _bus.Publish(
                new PasswordChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    user.Id,
                    user.Email,
                    display,
                    DateTime.UtcNow
                )
            );

            return new(true, "Contraseña actualizada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al restablecer la contraseña");
            return new(false, "Error al restablecer la contraseña");
        }
    }
}
