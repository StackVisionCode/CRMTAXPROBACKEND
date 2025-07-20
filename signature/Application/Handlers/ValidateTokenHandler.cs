using System.Security.Cryptography;
using System.Text;
using Application.Helpers;
using AutoMapper;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs.SignatureEvents;
using signature.Application.DTOs;
using Signature.Application.DTOs;
using signature.Infrastruture.Queries;

namespace signature.Application.Handlers;

public class ValidateTokenHandler
    : IRequestHandler<ValidateTokenQuery, ApiResponse<ValidateTokenResultDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<ValidateTokenHandler> _log;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly IEncryptionService _encryption;

    public ValidateTokenHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<ValidateTokenHandler> log,
        IMapper mapper,
        IEventBus eventBus,
        IEncryptionService encryption
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
        _mapper = mapper;
        _eventBus = eventBus;
        _encryption = encryption;
    }

    public async Task<ApiResponse<ValidateTokenResultDto>> Handle(
        ValidateTokenQuery request,
        CancellationToken cancellationToken
    )
    {
        /* 1 ▸ Valida JWT */
        (bool ok, Guid signerId, Guid reqId) = _tokenSvc.Validate(request.Token, "sign");
        if (!ok)
            return new(false, "Token inválido o expirado");

        /* 2 ▸ Busca solicitud + firmante */
        var req = await _db.SignatureRequests.FirstOrDefaultAsync(
            r => r.Id == reqId,
            cancellationToken
        );

        if (req is null)
            return new(false, "Solicitud no encontrada");

        var signer = await _db.Signers.FirstOrDefaultAsync(
            s => s.Id == signerId && s.SignatureRequestId == reqId,
            cancellationToken
        );

        if (signer is null)
            return new(false, "Firmante no encontrado para este token");

        if (req.Status == SignatureStatus.Rejected)
            return new(false, "La solicitud fue rechazada y ya no es válida.");

        // 2.1 Obtener las cajas del firmante usando JOIN explícito
        var signerBoxes = await _db
            .SignatureBoxes.Where(b => b.SignerId == signerId)
            .OrderBy(b => b.PageNumber)
            .ThenBy(b => b.PositionY)
            .ToListAsync(cancellationToken);

        // 2.2 Genera token de acceso temporal al documento
        var sessionId = GenerateSecureSessionId();
        var (documentAccessToken, expiresAt) = _tokenSvc.Generate(
            signerId,
            req.DocumentId,
            "document-access"
        );

        // 2.3 Crear payload sensible a cifrar
        var requestFingerprint = GenerateRequestFingerprint(req.Id, signerId, sessionId);
        var sensitivePayload = new DocumentAccessPayload(
            signerId,
            signer.Email ?? string.Empty,
            documentAccessToken,
            sessionId,
            requestFingerprint
        );

        try
        {
            // 2.4 Cifrar datos sensibles
            var encryptedPayload = _encryption.Encrypt(sensitivePayload);
            var payloadHash = ComputePayloadHash(sensitivePayload);

            // 2.5 Publicar evento seguro
            var secureEvent = new SecureDocumentAccessRequestedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                req.DocumentId,
                encryptedPayload,
                payloadHash,
                expiresAt
            );

            _eventBus.Publish(secureEvent);

            _log.LogInformation(
                "Evento seguro publicado para documento {DocumentId}, sesión {SessionId}",
                req.DocumentId,
                sessionId
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error publicando evento seguro para documento {DocumentId}",
                req.DocumentId
            );
            return new(false, "Error interno procesando solicitud");
        }

        /* 3 ▸ Construye DTO con las cajas obtenidas por separado */
        var dto = new ValidateTokenResultDto
        {
            SignatureRequestId = req.Id,
            DocumentId = req.DocumentId,
            DocumentAccessToken = documentAccessToken,
            SessionId = sessionId,
            RequestStatus = req.Status,
            /* mapeo actualizado usando las cajas obtenidas por separado */
            Signer = new SignerInfoDto
            {
                CustomerId = signer.CustomerId,
                Email = signer.Email!,
                Order = signer.Order,
                Status = signer.Status,
                FullName = signer.FullName,
                Boxes = signerBoxes
                    .Select(b => new SignatureBoxDto
                    {
                        SignerId = b.SignerId, // Agregar el SignerId
                        Page = b.PageNumber,
                        PosX = b.PositionX,
                        PosY = b.PositionY,
                        Width = b.Width,
                        Height = b.Height,
                        InitialEntity = b.InitialEntity is null
                            ? null
                            : new InitialEntityDto
                            {
                                InitalValue = b.InitialEntity.InitalValue,
                                PositionXIntial = b.InitialEntity.PositionXIntial,
                                PositionYIntial = b.InitialEntity.PositionYIntial,
                                WidthIntial = b.InitialEntity.WidthIntial,
                                HeightIntial = b.InitialEntity.HeightIntial,
                            },
                        FechaSigner = b.FechaSigner is null
                            ? null
                            : new FechaSignerDto
                            {
                                FechaValue = b.FechaSigner.FechaValue,
                                PositionXFechaSigner = b.FechaSigner.PositionXFechaSigner,
                                PositionYFechaSigner = b.FechaSigner.PositionYFechaSigner,
                                WidthFechaSigner = b.FechaSigner.WidthFechaSigner,
                                HeightFechaSigner = b.FechaSigner.HeightFechaSigner,
                            },
                    })
                    .ToList(),
            },
        };

        return new(true, "Token válido", dto);
    }

    private string GenerateSecureSessionId()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string GenerateRequestFingerprint(Guid requestId, Guid signerId, string sessionId)
    {
        var data = $"{requestId}:{signerId}:{sessionId}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private string ComputePayloadHash(DocumentAccessPayload payload)
    {
        var data =
            $"{payload.SignerId}:{payload.AccessToken}:{payload.SessionId}:{payload.RequestFingerprint}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
