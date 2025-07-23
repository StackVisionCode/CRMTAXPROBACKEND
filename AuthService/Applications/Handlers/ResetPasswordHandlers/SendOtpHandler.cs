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
            // 🔍 PASO 1: BUSCAR EN TAXUSERS CON JOIN EXPLÍCITO
            var taxUserData = await (
                from u in _db.TaxUsers
                where
                    u.Email == r.Email
                    && u.ResetPasswordToken != null
                    && u.ResetPasswordToken == r.Token
                    && u.ResetPasswordExpires > DateTime.UtcNow
                join p in _db.TaxUserProfiles on u.Id equals p.TaxUserId into prof
                from p in prof.DefaultIfEmpty()
                join co in _db.Companies on u.CompanyId equals co.Id into comp
                from co in comp.DefaultIfEmpty()
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    ResetPasswordToken = u.ResetPasswordToken,
                    ResetPasswordExpires = u.ResetPasswordExpires,
                    // Datos para display
                    ProfileName = p != null ? p.Name : null,
                    ProfileLastName = p != null ? p.LastName : null,
                    CompanyName = co != null ? co.CompanyName : null,
                    UserType = "TaxUser",
                }
            ).FirstOrDefaultAsync(ct);

            // 🔍 PASO 2: SI NO ESTÁ EN TAXUSERS, BUSCAR EN COMPANYUSERS
            var companyUserData =
                taxUserData == null
                    ? await (
                        from cu in _db.CompanyUsers
                        where
                            cu.Email == r.Email
                            && cu.ResetPasswordToken != null
                            && cu.ResetPasswordToken == r.Token
                            && cu.ResetPasswordExpires > DateTime.UtcNow
                        join cup in _db.CompanyUserProfiles
                            on cu.Id equals cup.CompanyUserId
                            into prof
                        from cup in prof.DefaultIfEmpty()
                        join co in _db.Companies on cu.CompanyId equals co.Id into comp
                        from co in comp.DefaultIfEmpty()
                        select new
                        {
                            UserId = cu.Id,
                            Email = cu.Email,
                            ResetPasswordToken = cu.ResetPasswordToken,
                            ResetPasswordExpires = cu.ResetPasswordExpires,
                            // Datos para display
                            ProfileName = cup != null ? cup.Name : null,
                            ProfileLastName = cup != null ? cup.LastName : null,
                            CompanyName = co != null ? co.CompanyName : null,
                            UserType = "CompanyUser",
                        }
                    ).FirstOrDefaultAsync(ct)
                    : null;

            // 📋 PASO 3: DETERMINAR QUÉ USUARIO ENCONTRAMOS
            var userData = taxUserData ?? companyUserData;
            if (userData is null)
                return new(false, "Solicitud inválida");

            // 🔐 PASO 4: VALIDAR TOKEN Y EXPIRACIÓN
            if (
                userData.ResetPasswordToken != r.Token
                || userData.ResetPasswordExpires < DateTime.UtcNow
            )
                return new(false, "Token expirado o inválido");

            // 🎲 PASO 5: GENERAR OTP
            var (otp, exp) = _otpSrv.Generate();

            // ✅ PASO 6: ACTUALIZAR USUARIO SEGÚN SU TIPO
            var updateResult = false;
            if (userData.UserType == "TaxUser")
            {
                updateResult =
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
            }
            else
            {
                updateResult =
                    await _db
                        .CompanyUsers.Where(cu => cu.Id == userData.UserId)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(cu => cu.Otp, otp)
                                    .SetProperty(cu => cu.OtpExpires, exp)
                                    .SetProperty(cu => cu.OtpVerified, false) // Reset verification status
                                    .SetProperty(cu => cu.UpdatedAt, DateTime.UtcNow),
                            ct
                        ) > 0;
            }

            if (!updateResult)
                return new(false, "Error al generar OTP");

            // 📝 PASO 7: CALCULAR DISPLAY NAME
            var display = DisplayNameHelper.From(
                userData.ProfileName,
                userData.ProfileLastName,
                userData.CompanyName,
                userData.Email
            );

            // 📧 PASO 8: PUBLICAR EVENTO DE OTP
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
                "OTP sent: Email={Email}, UserId={UserId}, UserType={UserType}",
                userData.Email,
                userData.UserId,
                userData.UserType
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
