using AuthService.Commands.ResetPasswordCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ResetPasswordHandlers;

public class ValidateOtpHandler : IRequestHandler<ValidateOtpCommands, ApiResponse<Unit>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ValidateOtpHandler> _logger;

    public ValidateOtpHandler(ApplicationDbContext db, ILogger<ValidateOtpHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<Unit>> Handle(ValidateOtpCommands r, CancellationToken ct)
    {
        try
        {
            // 🔍 BUSCAR USUARIO CON OTP VÁLIDO
            var userData = await (
                from u in _db.TaxUsers
                where u.Email == r.Email && u.Otp == r.Otp && u.OtpExpires > DateTime.UtcNow
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Otp = u.Otp,
                    OtpExpires = u.OtpExpires,
                }
            ).FirstOrDefaultAsync(ct);

            if (userData == null)
            {
                _logger.LogWarning(
                    "OTP validation failed: Invalid or expired OTP for {Email}",
                    r.Email
                );
                return new(false, "OTP inválido o expirado");
            }

            // ✅ MARCAR OTP COMO VERIFICADO
            var updateResult =
                await _db
                    .TaxUsers.Where(u => u.Id == userData.UserId)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(u => u.OtpExpires, DateTime.UtcNow) // Quemar OTP inmediatamente
                                .SetProperty(u => u.OtpVerified, true) // Marcar como verificado
                                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                        ct
                    ) > 0;

            if (!updateResult)
            {
                _logger.LogError("Failed to validate OTP for user: {Email}", userData.Email);
                return new(false, "Error al validar OTP");
            }

            _logger.LogInformation(
                "OTP validated successfully: Email={Email}, UserId={UserId}",
                userData.Email,
                userData.UserId
            );

            return new(true, "OTP validado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar OTP para {Email}", r.Email);
            return new(false, "Error al validar OTP");
        }
    }
}
