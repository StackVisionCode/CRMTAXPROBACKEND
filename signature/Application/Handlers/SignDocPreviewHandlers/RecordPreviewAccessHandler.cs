using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using signature.Application.Commands;

namespace signature.Application.Handlers;

public class RecordPreviewAccessHandler
    : IRequestHandler<RecordPreviewAccessCommand, ApiResponse<bool>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<RecordPreviewAccessHandler> _log;

    public RecordPreviewAccessHandler(
        SignatureDbContext db,
        ILogger<RecordPreviewAccessHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        RecordPreviewAccessCommand request,
        CancellationToken ct
    )
    {
        // Buscar preview
        var preview = await _db.SignPreviewDocuments.FirstOrDefaultAsync(
            p => p.AccessToken == request.AccessToken && p.SessionId == request.SessionId,
            ct
        );

        if (preview == null)
        {
            return new(false, "Preview no encontrado");
        }

        // Verificar si puede acceder
        if (!preview.CanAccess())
        {
            return new(false, "Acceso no permitido - expirado o límite alcanzado");
        }

        // Registrar acceso usando método de la entidad
        preview.RecordAccess(request.ClientIp, request.UserAgent);

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Acceso registrado para preview {PreviewId} - Total accesos: {AccessCount}",
            preview.Id,
            preview.AccessCount
        );

        return new(true, "Acceso registrado exitosamente");
    }
}
