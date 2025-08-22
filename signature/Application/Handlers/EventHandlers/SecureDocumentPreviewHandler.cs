using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs.SignatureEvents;

namespace Application.Handlers.EventHandlers;

/// <summary>
/// Handler que CONSUME el evento cuando el documento está sellado y listo
/// Similar a como EmailService consume el evento para enviar emails
/// </summary>
public sealed class SecureDocumentPreviewHandler
    : IIntegrationEventHandler<SecureDownloadSignedDocument>
{
    private readonly IEncryptionService _encryption;
    private readonly SignatureDbContext _db;
    private readonly ILogger<SecureDocumentPreviewHandler> _log;

    public SecureDocumentPreviewHandler(
        IEncryptionService encryption,
        SignatureDbContext db,
        ILogger<SecureDocumentPreviewHandler> log
    )
    {
        _encryption = encryption;
        _db = db;
        _log = log;
    }

    public async Task Handle(SecureDownloadSignedDocument e)
    {
        try
        {
            _log.LogInformation(
                "🚀 Procesando evento SecureDownloadSignedDocument para documento {SealedDocumentId}",
                e.SealedDocumentId
            );

            // 1. Descifrar payload
            DocumentAccessPayload payload;
            try
            {
                payload = _encryption.Decrypt<DocumentAccessPayload>(e.EncryptedPayload);
                _log.LogInformation(
                    "Payload descifrado exitosamente - SignerId: {SignerId}",
                    payload.SignerId
                );
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "❌ Error descifrando payload preview {Doc}", e.SealedDocumentId);
                return;
            }

            // 2. Verificar hash
            if (!VerifyHash(payload, e.PayloadHash))
            {
                _log.LogWarning("❌ Hash mismatch preview doc {Doc}", e.SealedDocumentId);
                return;
            }

            // 3. Verificar expiración
            if (e.ExpiresAt < DateTime.UtcNow)
            {
                _log.LogInformation("⏰ Acceso preview expirado {Doc}", e.SealedDocumentId);
                return;
            }

            // 4. Buscar información del firmante en nuestra DB
            var signer = await _db
                .Signers.Include(s => s.SignatureRequest)
                .FirstOrDefaultAsync(s => s.Id == payload.SignerId);

            if (signer == null)
            {
                _log.LogWarning(
                    "⚠️ Firmante con SignerId {SignerId} no encontrado para preview. "
                        + "Verificar que el SignerId del payload sea correcto.",
                    payload.SignerId
                );
                return;
            }

            _log.LogInformation(
                "Firmante encontrado: SignerId={SignerId}, CustomerId={CustomerId}, "
                    + "Email={Email}, Status={Status}, SignedAt={SignedAt}",
                signer.Id,
                signer.CustomerId,
                signer.Email,
                signer.Status,
                signer.SignedAtUtc
            );

            // 5. Verificar que el firmante haya firmado
            if (signer.Status != SignerStatus.Signed)
            {
                _log.LogInformation(
                    "⏳ Firmante {SignerId} no ha firmado aún (Status: {Status}), skip preview",
                    payload.SignerId,
                    signer.Status
                );
                return;
            }

            // 6. Buscar preview existente
            var existingPreview = await _db.SignPreviewDocuments.FirstOrDefaultAsync(p =>
                p.SignerId == payload.SignerId && p.SealedDocumentId == e.SealedDocumentId
            );

            if (existingPreview != null)
            {
                // Actualizar credenciales existentes
                existingPreview.UpdateAccessCredentials(
                    payload.AccessToken,
                    payload.SessionId,
                    payload.RequestFingerprint,
                    e.ExpiresAt
                );

                _log.LogInformation(
                    "🔄 Preview actualizado para SignerId {SignerId}, documento {SealedDocumentId}",
                    payload.SignerId,
                    e.SealedDocumentId
                );
            }
            else
            {
                // Crear nuevo acceso de preview
                var documentPreview = new SignPreviewDocument(
                    signatureRequestId: signer.SignatureRequestId,
                    signerId: payload.SignerId, // CRÍTICO: Usar el SignerId del payload
                    originalDocumentId: signer.SignatureRequest.DocumentId,
                    sealedDocumentId: e.SealedDocumentId,
                    accessToken: payload.AccessToken,
                    sessionId: payload.SessionId,
                    requestFingerprint: payload.RequestFingerprint,
                    expiresAt: e.ExpiresAt,
                    maxAccessCount: 10 // Aumentado para desarrollo
                );

                _db.SignPreviewDocuments.Add(documentPreview);

                _log.LogInformation(
                    "🆕 Preview creado para SignerId {SignerId}, documento {SealedDocumentId}, "
                        + "AccessToken: {AccessToken}, SessionId: {SessionId}",
                    payload.SignerId,
                    e.SealedDocumentId,
                    payload.AccessToken.Substring(0, 8) + "...",
                    payload.SessionId.Substring(0, 8) + "..."
                );
            }

            // 7. Guardar cambios
            await _db.SaveChangesAsync();

            _log.LogInformation(
                "Preview access preparado exitosamente para SignerId {SignerId}",
                payload.SignerId
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "❌ Error procesando preview para documento {Doc}",
                e.SealedDocumentId
            );
        }
    }

    private bool VerifyHash(DocumentAccessPayload p, string expected)
    {
        var data = $"{p.SignerId}:{p.AccessToken}:{p.SessionId}:{p.RequestFingerprint}";
        using var sha = SHA256.Create();
        var h = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
        return string.Equals(h, expected, StringComparison.OrdinalIgnoreCase);
    }
}
