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
            var user = await _db
                .TaxUsers.Include(u => u.TaxUserProfile)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == r.Email, ct);

            if (user is null || user.ResetPasswordToken is null)
                return new(false, "Solicitud inválida");

            // 1. Token proporcionado coincide y sigue vigente
            if (user.ResetPasswordToken != r.Token || user.ResetPasswordExpires < DateTime.UtcNow)
                return new(false, "Token expirado o inválido");

            var (otp, exp) = _otpSrv.Generate();
            user.Otp = otp;
            user.OtpExpires = exp;
            await _db.SaveChangesAsync(ct);

            var display = DisplayNameHelper.From(
                user.TaxUserProfile?.Name,
                user.TaxUserProfile?.LastName,
                user.Company?.CompanyName,
                user.Email
            );

            _bus.Publish(
                new PasswordResetOtpEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    user.Id,
                    user.Email,
                    display,
                    otp,
                    exp
                )
            );

            return new(true, "OTP enviado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar OTP");
            return new(false, "Error al enviar OTP");
        }
    }
}
