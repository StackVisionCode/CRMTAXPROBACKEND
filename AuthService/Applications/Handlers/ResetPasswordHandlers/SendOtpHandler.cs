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

public class SendOtpHandler : IRequestHandler<SendOtpCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly IOtpService _otpSrv;
    private readonly IEventBus _bus;
    private readonly ILogger<SendOtpHandler> _logger;

    public SendOtpHandler(
        ApplicationDbContext db,
        IOtpService otpSrv,
        IEventBus bus,
        ILogger<SendOtpHandler> logger
    )
    {
        _db = db;
        _otpSrv = otpSrv;
        _bus = bus;
        _logger = logger;
    }

    public async Task<ApiResponse<Unit>> Handle(SendOtpCommands r, CancellationToken ct)
    {
        try
        {
            // 🔍 BUSCAR USUARIO CON TOKEN VÁLIDO
            var userData = await (
                from u in _db.TaxUsers
                where
                    u.Email == r.Email
                    && u.ResetPasswordToken != null
                    && u.ResetPasswordToken == r.Token
                    && u.ResetPasswordExpires > DateTime.UtcNow
                join c in _db.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
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
                _logger.LogWarning("Send OTP failed: Invalid request for {Email}", r.Email);
                return new(false, "Solicitud inválida");
            }

            // 🔐 VALIDAR TOKEN Y EXPIRACIÓN
            if (
                userData.ResetPasswordToken != r.Token
                || userData.ResetPasswordExpires < DateTime.UtcNow
            )
            {
                _logger.LogWarning(
                    "Send OTP failed: Invalid or expired token for {Email}",
                    r.Email
                );
                return new(false, "Token expirado o inválido");
            }

            // 🎲 GENERAR OTP
            var (otp, exp) = _otpSrv.Generate();

            // ✅ ACTUALIZAR USUARIO CON OTP
            var updateResult =
                await _db
                    .TaxUsers.Where(u => u.Id == userData.UserId)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(u => u.Otp, otp)
                                .SetProperty(u => u.OtpExpires, exp)
                                .SetProperty(u => u.OtpVerified, false) // Reset verification status
                                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                        ct
                    ) > 0;

            if (!updateResult)
            {
                _logger.LogError("Failed to generate OTP for user: {Email}", userData.Email);
                return new(false, "Error al generar OTP");
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

            // 📧 PUBLICAR EVENTO DE OTP
            _bus.Publish(
                new PasswordResetOtpEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userData.UserId,
                    userData.Email,
                    display,
                    otp,
                    exp
                )
            );

            _logger.LogInformation(
                "OTP sent: Email={Email}, UserId={UserId}",
                userData.Email,
                userData.UserId
            );

            return new(true, "OTP enviado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar OTP para {Email}", r.Email);
            return new(false, "Error al enviar OTP");
        }
    }
}
