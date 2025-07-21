using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastructure.Queries.CustomerSignatureQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler para obtener actividad de una solicitud específica
/// </summary>
public class GetSignatureActivityHandler
    : IRequestHandler<GetSignatureActivityQuery, ApiResponse<List<SignatureActivityDto>>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetSignatureActivityHandler> _logger;

    public GetSignatureActivityHandler(
        SignatureDbContext db,
        ILogger<GetSignatureActivityHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SignatureActivityDto>>> Handle(
        GetSignatureActivityQuery request,
        CancellationToken ct
    )
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo actividad para solicitud {RequestId}",
                request.RequestId
            );

            // Crear actividad sintética basada en los datos existentes
            var activities = new List<SignatureActivityDto>();

            // Obtener la solicitud y sus firmantes
            var requestData = await (
                from r in _db.SignatureRequests
                join s in _db.Signers on r.Id equals s.SignatureRequestId
                where r.Id == request.RequestId
                select new { Request = r, Signer = s }
            ).ToListAsync(ct);

            if (!requestData.Any())
            {
                return ApiResponse<List<SignatureActivityDto>>.Ok(new List<SignatureActivityDto>());
            }

            var signatureRequest = requestData.First().Request;
            var signers = requestData.Select(x => x.Signer).ToList();

            // Actividad: Solicitud creada
            activities.Add(
                new SignatureActivityDto
                {
                    Id = Guid.NewGuid(),
                    SignatureRequestId = request.RequestId,
                    ActivityType = ActivityType.RequestCreated.ToString(),
                    Description = "Solicitud de firma creada",
                    Timestamp = signatureRequest.CreatedAt,
                }
            );

            // Actividades por firmante
            foreach (var signer in signers.OrderBy(s => s.Order))
            {
                // Documento enviado
                activities.Add(
                    new SignatureActivityDto
                    {
                        Id = Guid.NewGuid(),
                        SignatureRequestId = request.RequestId,
                        SignerId = signer.Id,
                        SignerName = signer.FullName ?? signer.Email,
                        SignerEmail = signer.Email,
                        ActivityType = ActivityType.DocumentSent.ToString(),
                        Description = $"Documento enviado a {signer.FullName ?? signer.Email}",
                        Timestamp = signer.CreatedAt,
                    }
                );

                // Si firmó
                if (signer.Status == SignerStatus.Signed && signer.SignedAtUtc.HasValue)
                {
                    activities.Add(
                        new SignatureActivityDto
                        {
                            Id = Guid.NewGuid(),
                            SignatureRequestId = request.RequestId,
                            SignerId = signer.Id,
                            SignerName = signer.FullName ?? signer.Email,
                            SignerEmail = signer.Email,
                            ActivityType = ActivityType.DocumentSigned.ToString(),
                            Description = $"{signer.FullName ?? signer.Email} firmó el documento",
                            Timestamp = signer.SignedAtUtc.Value,
                        }
                    );
                }

                // Si rechazó
                if (signer.Status == SignerStatus.Rejected && signer.RejectedAtUtc.HasValue)
                {
                    activities.Add(
                        new SignatureActivityDto
                        {
                            Id = Guid.NewGuid(),
                            SignatureRequestId = request.RequestId,
                            SignerId = signer.Id,
                            SignerName = signer.FullName ?? signer.Email,
                            SignerEmail = signer.Email,
                            ActivityType = ActivityType.DocumentRejected.ToString(),
                            Description =
                                $"{signer.FullName ?? signer.Email} rechazó el documento"
                                + (
                                    string.IsNullOrEmpty(signer.RejectReason)
                                        ? ""
                                        : $": {signer.RejectReason}"
                                ),
                            Timestamp = signer.RejectedAtUtc.Value,
                        }
                    );
                }
            }

            // Solicitud completada
            if (
                signatureRequest.Status == SignatureStatus.Completed
                && signatureRequest.UpdatedAt.HasValue
            )
            {
                activities.Add(
                    new SignatureActivityDto
                    {
                        Id = Guid.NewGuid(),
                        SignatureRequestId = request.RequestId,
                        ActivityType = ActivityType.RequestCompleted.ToString(),
                        Description = "Solicitud de firma completada exitosamente",
                        Timestamp = signatureRequest.UpdatedAt.Value,
                    }
                );
            }

            // Ordenar por timestamp descendente (más reciente primero)
            var sortedActivities = activities.OrderByDescending(a => a.Timestamp).ToList();

            _logger.LogInformation(
                "Generadas {Count} actividades para solicitud {RequestId}",
                sortedActivities.Count,
                request.RequestId
            );

            return ApiResponse<List<SignatureActivityDto>>.Ok(sortedActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo actividad para solicitud {RequestId}",
                request.RequestId
            );
            return ApiResponse<List<SignatureActivityDto>>.Fail("Error interno del servidor");
        }
    }
}
