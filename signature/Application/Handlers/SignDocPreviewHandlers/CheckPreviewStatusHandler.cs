using Application.DTOs;
using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using signature.Application.Queries;

namespace signature.Application.Handlers;

public class CheckPreviewStatusHandler
    : IRequestHandler<CheckPreviewStatusQuery, ApiResponse<DocumentPreviewStatusDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<CheckPreviewStatusHandler> _log;

    public CheckPreviewStatusHandler(SignatureDbContext db, ILogger<CheckPreviewStatusHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<DocumentPreviewStatusDto>> Handle(
        CheckPreviewStatusQuery request,
        CancellationToken ct
    )
    {
        // Buscar preview con las credenciales
        var preview = await _db.SignPreviewDocuments.FirstOrDefaultAsync(
            p => p.AccessToken == request.AccessToken && p.SessionId == request.SessionId,
            ct
        );

        if (preview == null)
        {
            return new(
                false,
                "Preview no encontrado",
                new DocumentPreviewStatusDto
                {
                    CanPreview = false,
                    IsExpired = true,
                    AccessExpired = false,
                    RemainingAccess = 0,
                    ExpiresAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    StatusMessage = "Credenciales de acceso inválidas",
                    ErrorCode = "INVALID_CREDENTIALS",
                }
            );
        }

        // Evaluar estado del preview
        var canAccess = preview.CanAccess();
        var isExpired = preview.ExpiresAt <= DateTime.UtcNow;
        var accessExpired = preview.AccessCount >= preview.MaxAccessCount;
        var remainingAccess = Math.Max(0, preview.MaxAccessCount - preview.AccessCount);

        string statusMessage;
        string? errorCode = null;

        if (!preview.IsActive)
        {
            statusMessage = "El acceso ha sido desactivado";
            errorCode = "ACCESS_DEACTIVATED";
        }
        else if (isExpired)
        {
            statusMessage = "El acceso ha expirado por tiempo";
            errorCode = "TIME_EXPIRED";
        }
        else if (accessExpired)
        {
            statusMessage = "Se ha alcanzado el límite máximo de accesos";
            errorCode = "ACCESS_LIMIT_REACHED";
        }
        else if (canAccess)
        {
            statusMessage = $"Acceso válido. {remainingAccess} visualizaciones restantes";
        }
        else
        {
            statusMessage = "Acceso no válido";
            errorCode = "ACCESS_DENIED";
        }

        var result = new DocumentPreviewStatusDto
        {
            CanPreview = canAccess,
            IsExpired = isExpired,
            AccessExpired = accessExpired,
            RemainingAccess = remainingAccess,
            ExpiresAt = preview.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            StatusMessage = statusMessage,
            ErrorCode = errorCode,
        };

        _log.LogInformation(
            "Estado de preview verificado - CanPreview: {CanPreview}, Message: {Message}",
            canAccess,
            statusMessage
        );

        return new(true, "Estado verificado", result);
    }
}
