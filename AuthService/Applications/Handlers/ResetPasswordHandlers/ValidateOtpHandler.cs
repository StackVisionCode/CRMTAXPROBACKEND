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
            // 🔍 PASO 1: BUSCAR EN TAXUSERS
            var taxUserData = await (
                from u in _db.TaxUsers
                where u.Email == r.Email && u.Otp == r.Otp && u.OtpExpires > DateTime.UtcNow
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Otp = u.Otp,
                    OtpExpires = u.OtpExpires,
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
                            && cu.Otp == r.Otp
                            && cu.OtpExpires > DateTime.UtcNow
                        select new
                        {
                            UserId = cu.Id,
                            Email = cu.Email,
                            Otp = cu.Otp,
                            OtpExpires = cu.OtpExpires,
                            UserType = "CompanyUser",
                        }
                    ).FirstOrDefaultAsync(ct)
                    : null;

            // 📋 PASO 3: DETERMINAR QUÉ USUARIO ENCONTRAMOS
            var userData = taxUserData ?? companyUserData;
            if (userData is null)
                return new(false, "OTP inválido o expirado");

            // ✅ PASO 4: MARCAR OTP COMO VERIFICADO SEGÚN TIPO DE USUARIO
            var updateResult = false;
            if (userData.UserType == "TaxUser")
            {
                updateResult =
                    await _db
                        .TaxUsers.Where(u => u.Id == userData.UserId)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(u => u.OtpExpires, DateTime.UtcNow) // Quemar OTP inmediatamente
                                    .SetProperty(u => u.OtpVerified, true) // Marcar como verificado
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
                                s.SetProperty(cu => cu.OtpExpires, DateTime.UtcNow) // Quemar OTP inmediatamente
                                    .SetProperty(cu => cu.OtpVerified, true) // Marcar como verificado
                                    .SetProperty(cu => cu.UpdatedAt, DateTime.UtcNow),
                            ct
                        ) > 0;
            }

            if (!updateResult)
                return new(false, "Error al validar OTP");

            _logger.LogInformation(
                "OTP validated successfully: Email={Email}, UserId={UserId}, UserType={UserType}",
                userData.Email,
                userData.UserId,
                userData.UserType
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
