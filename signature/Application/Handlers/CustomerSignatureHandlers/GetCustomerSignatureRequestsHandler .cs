using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastructure.Queries.CustomerSignatureQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.CustomerSignature;

/// <summary>
/// Handler para obtener solicitudes de firma de un cliente
/// </summary>
public class GetCustomerSignatureRequestsHandler
    : IRequestHandler<
        GetCustomerSignatureRequestsQuery,
        ApiResponse<List<CustomerSignatureRequestDto>>
    >
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetCustomerSignatureRequestsHandler> _logger;

    public GetCustomerSignatureRequestsHandler(
        SignatureDbContext db,
        ILogger<GetCustomerSignatureRequestsHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CustomerSignatureRequestDto>>> Handle(
        GetCustomerSignatureRequestsQuery request,
        CancellationToken ct
    )
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo solicitudes para cliente {CustomerId}",
                request.CustomerId
            );

            var requests = await (
                from r in _db.SignatureRequests
                join s in _db.Signers on r.Id equals s.SignatureRequestId
                where s.CustomerId == request.CustomerId
                group new { r, s } by new
                {
                    r.Id,
                    r.DocumentId,
                    r.Status,
                    r.CreatedAt,
                    r.UpdatedAt,
                    r.RejectedBySignerId,
                    r.RejectReason,
                    r.RejectedAtUtc,
                } into g
                select new CustomerSignatureRequestDto
                {
                    Id = g.Key.Id,
                    DocumentId = g.Key.DocumentId,
                    DocumentTitle =
                        $"Documento {new string(g.Key.DocumentId.ToString().Take(8).ToArray())}...",
                    CustomerId = request.CustomerId,
                    CustomerName = "Cliente", // Obtener del servicio de customers en el futuro
                    Status = g.Key.Status,
                    CreatedAt = g.Key.CreatedAt,
                    UpdatedAt = g.Key.UpdatedAt,
                    CompletedAt =
                        g.Key.Status == SignatureStatus.Completed ? g.Key.UpdatedAt : null,
                    TotalSigners = g.Count(),
                    CompletedSigners = g.Count(x => x.s.Status == SignerStatus.Signed),
                    RejectedBySignerId = g.Key.RejectedBySignerId,
                    RejectReason = g.Key.RejectReason,
                    RejectedAtUtc = g.Key.RejectedAtUtc,
                    Signers = g.Select(x => new CustomerSignerDto
                        {
                            Id = x.s.Id,
                            CustomerId = x.s.CustomerId,
                            FullName = x.s.FullName ?? x.s.Email,
                            Email = x.s.Email,
                            Status = x.s.Status,
                            Order = x.s.Order,
                            SignedAtUtc = x.s.SignedAtUtc,
                            RejectedAtUtc = x.s.RejectedAtUtc,
                            RejectReason = x.s.RejectReason,
                            AccessCount = 0, // Calcular si es necesario
                            RemindersSent = 0, // Calcular si es necesario
                            CreatedAt = x.s.CreatedAt,
                            TimeToSign =
                                x.s.SignedAtUtc.HasValue && x.s.CreatedAt != default
                                    ? (int)(x.s.SignedAtUtc.Value - x.s.CreatedAt).TotalHours
                                    : null,
                        })
                        .ToList(),
                }
            ).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

            // Calcular IsCurrentTurn para cada firmante
            foreach (var req in requests)
            {
                foreach (var signer in req.Signers)
                {
                    signer.IsCurrentTurn = DetermineCurrentTurn(signer, req.Signers);
                }
            }

            _logger.LogInformation(
                "Encontradas {Count} solicitudes para cliente {CustomerId}",
                requests.Count,
                request.CustomerId
            );

            return ApiResponse<List<CustomerSignatureRequestDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo solicitudes para cliente {CustomerId}",
                request.CustomerId
            );
            return ApiResponse<List<CustomerSignatureRequestDto>>.Fail(
                "Error interno del servidor"
            );
        }
    }

    private static bool DetermineCurrentTurn(
        CustomerSignerDto signer,
        List<CustomerSignerDto> allSigners
    )
    {
        // Si ya firmó o rechazó, no es su turno
        if (signer.Status == SignerStatus.Signed || signer.Status == SignerStatus.Rejected)
            return false;

        // Si es firma secuencial, verificar que todos los anteriores estén firmados
        var previousSigners = allSigners.Where(s => s.Order < signer.Order);
        return previousSigners.All(s => s.Status == SignerStatus.Signed);
    }
}
