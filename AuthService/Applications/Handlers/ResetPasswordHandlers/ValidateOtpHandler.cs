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
            var user = await _db.TaxUsers.FirstOrDefaultAsync(u => u.Email == r.Email, ct);
            if (user is null || user.Otp != r.Otp || user.OtpExpires < DateTime.UtcNow)
                return new(false, "OTP inválido o expirado");

            // Marcamos OTP como usada
            user.OtpExpires = DateTime.UtcNow; // quema inmediata
            user.OtpVerified = true; // marcamos como verificado
            await _db.SaveChangesAsync(ct);
            return new(true, "OTP validado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar OTP");
            return new(false, "Error al validar OTP");
        }
    }
}
