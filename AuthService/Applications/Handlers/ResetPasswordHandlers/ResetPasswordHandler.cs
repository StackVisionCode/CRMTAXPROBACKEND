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
            // 🔍 PASO 1: BUSCAR EN TAXUSERS CON VALIDACIONES COMPLETAS
            var taxUserData = await (
                from u in _db.TaxUsers
                where
                    u.Email == r.Email
                    && u.OtpVerified == true
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
                    OtpVerified = u.OtpVerified,
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
                            && cu.OtpVerified == true
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
                            OtpVerified = cu.OtpVerified,
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
                return new(false, "Usuario no encontrado");

            // 🔐 PASO 4: VALIDACIONES DE SEGURIDAD
            if (userData.OtpVerified == false)
                return new(false, "OTP no verificado aún");

            if (
                userData.ResetPasswordToken != r.Token
                || userData.ResetPasswordExpires < DateTime.UtcNow
            )
                return new(false, "Token expirado o inválido");

            // 🔒 PASO 5: HASHEAR NUEVA CONTRASEÑA
            var hashedPassword = _passwordHash.HashPassword(r.NewPassword);

            // ✅ PASO 6: ACTUALIZAR CONTRASEÑA Y LIMPIAR TOKENS SEGÚN TIPO DE USUARIO
            var updateResult = false;
            if (userData.UserType == "TaxUser")
            {
                updateResult =
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
            }
            else
            {
                updateResult =
                    await _db
                        .CompanyUsers.Where(cu => cu.Id == userData.UserId)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(cu => cu.Password, hashedPassword)
                                    .SetProperty(cu => cu.ResetPasswordToken, (string?)null) // Limpiar token
                                    .SetProperty(cu => cu.ResetPasswordExpires, (DateTime?)null) // Limpiar expiración
                                    .SetProperty(cu => cu.OtpVerified, false) // Reset OTP verification
                                    .SetProperty(cu => cu.Otp, (string?)null) // Limpiar OTP
                                    .SetProperty(cu => cu.OtpExpires, (DateTime?)null) // Limpiar expiración OTP
                                    .SetProperty(cu => cu.UpdatedAt, DateTime.UtcNow),
                            ct
                        ) > 0;
            }

            if (!updateResult)
                return new(false, "Error al actualizar la contraseña");

            // 📝 PASO 7: CALCULAR DISPLAY NAME
            var display = DisplayNameHelper.From(
                userData.ProfileName,
                userData.ProfileLastName,
                userData.CompanyName,
                userData.Email
            );

            // 📧 PASO 8: PUBLICAR EVENTO DE CONTRASEÑA CAMBIADA
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
                "Password reset successfully: Email={Email}, UserId={UserId}, UserType={UserType}",
                userData.Email,
                userData.UserId,
                userData.UserType
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
