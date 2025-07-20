using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using signature.Application.Commands;

namespace signature.Application.Handlers;

public class InvalidatePreviewAccessHandler
    : IRequestHandler<InvalidatePreviewAccessCommand, ApiResponse<bool>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<InvalidatePreviewAccessHandler> _log;

    public InvalidatePreviewAccessHandler(
        SignatureDbContext db,
        ILogger<InvalidatePreviewAccessHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        InvalidatePreviewAccessCommand request,
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
            // No existe, pero devolvemos success para idempotencia
            return new(true, "Preview no encontrado o ya invalidado");
        }

        // Desactivar usando método de la entidad
        preview.Deactivate();

        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Preview {PreviewId} invalidado exitosamente", preview.Id);

        return new(true, "Acceso invalidado exitosamente");
    }
}
