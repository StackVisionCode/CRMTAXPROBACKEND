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

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _bus;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        ApplicationDbContext db,
        IPasswordHash passwordHash,
        IEventBus bus,
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
            // 🔍 BUSCAR USUARIO CON VALIDACIONES COMPLETAS
            var userData = await (
                from u in _db.TaxUsers
                where
                    u.Email == r.Email
                    && u.OtpVerified == true
                    && u.ResetPasswordToken == r.Token
                    && u.ResetPasswordExpires > DateTime.UtcNow
                join c in _db.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    OtpVerified = u.OtpVerified,
                    ResetPasswordToken = u.ResetPasswordToken,
                    ResetPasswordExpires = u.ResetPasswordExpires,
                    // Datos del usuario
                    Name = u.Name,
                    LastName = u.LastName,
                    // Datos de la company
                    CompanyFullName = c != null ? c.FullName : null,
                    CompanyName = c != null ? c.CompanyName : null,
                    IsCompany = c != null ? c.IsCompany : false,
                }
            ).FirstOrDefaultAsync(ct);

            if (userData == null)
            {
                _logger.LogWarning(
                    "Reset password failed: User not found or invalid conditions for {Email}",
                    r.Email
                );
                return new(false, "Usuario no encontrado o condiciones inválidas");
            }

            // 🔐 VALIDACIONES DE SEGURIDAD
            if (userData.OtpVerified == false)
            {
                _logger.LogWarning("Reset password failed: OTP not verified for {Email}", r.Email);
                return new(false, "OTP no verificado aún");
            }

            if (
                userData.ResetPasswordToken != r.Token
                || userData.ResetPasswordExpires < DateTime.UtcNow
            )
            {
                _logger.LogWarning(
                    "Reset password failed: Invalid or expired token for {Email}",
                    r.Email
                );
                return new(false, "Token expirado o inválido");
            }

            // 🔒 HASHEAR NUEVA CONTRASEÑA
            var hashedPassword = _passwordHash.HashPassword(r.NewPassword);

            // ACTUALIZAR CONTRASEÑA Y LIMPIAR TOKENS
            var updateResult =
                await _db
                    .TaxUsers.Where(u => u.Id == userData.UserId)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(u => u.Password, hashedPassword)
                                .SetProperty(u => u.ResetPasswordToken, (string?)null) // Limpiar token
                                .SetProperty(u => u.ResetPasswordExpires, (DateTime?)null) // Limpiar expiración
                                .SetProperty(u => u.OtpVerified, false) // Reset OTP verification
                                .SetProperty(u => u.Otp, (string?)null) // Limpiar OTP
                                .SetProperty(u => u.OtpExpires, (DateTime?)null) // Limpiar expiración OTP
                                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                        ct
                    ) > 0;

            if (!updateResult)
            {
                _logger.LogError("Failed to update password for user: {Email}", userData.Email);
                return new(false, "Error al actualizar la contraseña");
            }

            // 📝 CALCULAR DISPLAY NAME
            var display = DisplayNameHelper.From(
                userData.Name,
                userData.LastName,
                userData.CompanyFullName,
                userData.CompanyName,
                userData.IsCompany,
                userData.Email
            );

            // 📧 PUBLICAR EVENTO DE CONTRASEÑA CAMBIADA
            _bus.Publish(
                new PasswordChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userData.UserId,
                    userData.Email,
                    display,
                    DateTime.UtcNow
                )
            );

            _logger.LogInformation(
                "Password reset successfully: Email={Email}, UserId={UserId}",
                userData.Email,
                userData.UserId
            );

            return new(true, "Contraseña actualizada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al restablecer la contraseña para {Email}", r.Email);
            return new(false, "Error al restablecer la contraseña");
        }
    }
}
