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
            // 🔍 PASO 1: BUSCAR EN TAXUSERS CON JOIN EXPLÍCITO
            var taxUserData = await (
                from u in _db.TaxUsers
                where u.Email == r.Email && u.IsActive
                join p in _db.TaxUserProfiles on u.Id equals p.TaxUserId into prof
                from p in prof.DefaultIfEmpty()
                join co in _db.Companies on u.CompanyId equals co.Id into comp
                from co in comp.DefaultIfEmpty()
                select new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    // Datos para display
                    ProfileName = p != null ? p.Name : null,
                    ProfileLastName = p != null ? p.LastName : null,
                    CompanyName = co != null ? co.CompanyName : null,
                    Domain = u.Domain,
                    UserType = "TaxUser",
                }
            ).FirstOrDefaultAsync(ct);

            // 🔍 PASO 2: SI NO ESTÁ EN TAXUSERS, BUSCAR EN COMPANYUSERS
            var companyUserData =
                taxUserData == null
                    ? await (
                        from cu in _db.CompanyUsers
                        where cu.Email == r.Email && cu.IsActive
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
                            IsActive = cu.IsActive,
                            // Datos para display
                            ProfileName = cup != null ? cup.Name : null,
                            ProfileLastName = cup != null ? cup.LastName : null,
                            CompanyName = co != null ? co.CompanyName : null,
                            Domain = (string?)null,
                            UserType = "CompanyUser",
                        }
                    ).FirstOrDefaultAsync(ct)
                    : null;

            // 📋 PASO 3: DETERMINAR QUÉ USUARIO ENCONTRAMOS
            var userData = taxUserData ?? companyUserData;
            if (userData is null || !userData.IsActive)
                return new(false, "Cuenta no encontrada o inactiva");

            // 🔑 PASO 4: GENERAR TOKEN DE RESET
            var (token, exp) = _tokenSrv.Generate(userData.UserId, userData.Email);

            // ✅ PASO 5: ACTUALIZAR USUARIO SEGÚN SU TIPO
            var updateResult = false;
            if (userData.UserType == "TaxUser")
            {
                updateResult =
                    await _db
                        .TaxUsers.Where(u => u.Id == userData.UserId)
                        .ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(u => u.ResetPasswordToken, token)
                                    .SetProperty(u => u.ResetPasswordExpires, exp)
                                    .SetProperty(u => u.Otp, (string?)null) // invalidar OTP previa
                                    .SetProperty(u => u.OtpExpires, (DateTime?)null)
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
                                s.SetProperty(cu => cu.ResetPasswordToken, token)
                                    .SetProperty(cu => cu.ResetPasswordExpires, exp)
                                    .SetProperty(cu => cu.Otp, (string?)null) // invalidar OTP previa
                                    .SetProperty(cu => cu.OtpExpires, (DateTime?)null)
                                    .SetProperty(cu => cu.UpdatedAt, DateTime.UtcNow),
                            ct
                        ) > 0;
            }

            if (!updateResult)
                return new(false, "Error al procesar la solicitud de reset");

            // 🔗 PASO 6: GENERAR LINK DE RESET
            var link =
                $"{r.Origin.TrimEnd('/')}/auth/reset-password/validate?email={Uri.EscapeDataString(userData.Email)}&token={Uri.EscapeDataString(token)}";

            // 📝 PASO 7: CALCULAR DISPLAY NAME
            var display = DisplayNameHelper.From(
                userData.ProfileName,
                userData.ProfileLastName,
                userData.CompanyName,
                userData.Email
            );

            // 📧 PASO 8: PUBLICAR EVENTO DE RESET
            _bus.Publish(
                new PasswordResetLinkEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userData.UserId,
                    userData.Email,
                    display,
                    link,
                    exp
                )
            );

            _log.LogInformation(
                "Password reset link sent: Email={Email}, UserId={UserId}, UserType={UserType}",
                userData.Email,
                userData.UserId,
                userData.UserType
            );

            return new(true, "Se envió un correo con el enlace para reestablecer contraseña");
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error al procesar solicitud de reseteo de contraseña para {Email}",
                r.Email
            );
            return new(false, "Error al procesar la solicitud, intente más tarde");
        }
    }
}
