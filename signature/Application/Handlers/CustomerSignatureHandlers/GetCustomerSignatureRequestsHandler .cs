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

            // Primero obtener los IDs de las solicitudes donde participa el cliente
            var requestIds = await _db
                .Signers.Where(s => s.CustomerId == request.CustomerId)
                .Select(s => s.SignatureRequestId)
                .Distinct()
                .ToListAsync(ct);

            if (!requestIds.Any())
            {
                _logger.LogInformation(
                    "No se encontraron solicitudes para cliente {CustomerId}",
                    request.CustomerId
                );
                return ApiResponse<List<CustomerSignatureRequestDto>>.Ok(
                    new List<CustomerSignatureRequestDto>()
                );
            }

            // Ahora obtener todas las solicitudes completas (incluyendo firmantes externos)
            var requests = await (
                from r in _db.SignatureRequests
                where requestIds.Contains(r.Id)
                select new CustomerSignatureRequestDto
                {
                    Id = r.Id,
                    DocumentId = r.DocumentId,
                    DocumentTitle =
                        $"Documento {new string(r.DocumentId.ToString().Take(8).ToArray())}...",
                    CustomerId = request.CustomerId,
                    CustomerName = "Cliente", // Obtener del servicio de customers en el futuro
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CompletedAt = r.Status == SignatureStatus.Completed ? r.UpdatedAt : null,
                    RejectedBySignerId = r.RejectedBySignerId,
                    RejectReason = r.RejectReason,
                    RejectedAtUtc = r.RejectedAtUtc,
                    Signers = (
                        from s in _db.Signers
                        where s.SignatureRequestId == r.Id
                        select new CustomerSignerDto
                        {
                            Id = s.Id,
                            CustomerId = s.CustomerId,
                            FullName = s.FullName ?? s.Email,
                            Email = s.Email,
                            Status = s.Status,
                            Order = s.Order,
                            SignedAtUtc = s.SignedAtUtc,
                            RejectedAtUtc = s.RejectedAtUtc,
                            RejectReason = s.RejectReason,
                            CreatedAt = s.CreatedAt,
                            TimeToSign =
                                s.SignedAtUtc.HasValue && s.CreatedAt != default
                                    ? (int)(s.SignedAtUtc.Value - s.CreatedAt).TotalHours
                                    : null,
                        }
                    ).OrderBy(s => s.Order).ToList(),
                }
            ).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

            // Calcular propiedades derivadas
            foreach (var req in requests)
            {
                req.TotalSigners = req.Signers.Count;
                req.CompletedSigners = req.Signers.Count(s => s.Status == SignerStatus.Signed);

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
