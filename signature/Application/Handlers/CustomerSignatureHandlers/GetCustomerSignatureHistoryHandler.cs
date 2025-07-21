using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastructure.Queries.CustomerSignatureQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler para obtener historial de firmas de un cliente
/// </summary>
public class GetCustomerSignatureHistoryHandler
    : IRequestHandler<
        GetCustomerSignatureHistoryQuery,
        ApiResponse<List<CustomerSignatureRequestDto>>
    >
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetCustomerSignatureHistoryHandler> _logger;

    public GetCustomerSignatureHistoryHandler(
        SignatureDbContext db,
        ILogger<GetCustomerSignatureHistoryHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CustomerSignatureRequestDto>>> Handle(
        GetCustomerSignatureHistoryQuery request,
        CancellationToken ct
    )
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo historial para cliente {CustomerId}",
                request.CustomerId
            );

            // Para historial, incluimos TODAS las solicitudes, no solo las activas
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
                    CustomerName = "Cliente",
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
                            AccessCount = 0,
                            RemindersSent = 0,
                            CreatedAt = x.s.CreatedAt,
                            TimeToSign =
                                x.s.SignedAtUtc.HasValue && x.s.CreatedAt != default
                                    ? (int)(x.s.SignedAtUtc.Value - x.s.CreatedAt).TotalHours
                                    : null,
                        })
                        .ToList(),
                }
            ).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

            _logger.LogInformation(
                "Encontradas {Count} solicitudes en historial para cliente {CustomerId}",
                requests.Count,
                request.CustomerId
            );

            return ApiResponse<List<CustomerSignatureRequestDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo historial para cliente {CustomerId}",
                request.CustomerId
            );
            return ApiResponse<List<CustomerSignatureRequestDto>>.Fail(
                "Error interno del servidor"
            );
        }
    }
}
