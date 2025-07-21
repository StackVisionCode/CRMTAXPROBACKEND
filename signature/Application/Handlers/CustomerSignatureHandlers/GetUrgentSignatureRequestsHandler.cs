using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastructure.Queries.CustomerSignatureQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler para obtener solicitudes urgentes de un cliente
/// </summary>
public class GetUrgentSignatureRequestsHandler
    : IRequestHandler<
        GetUrgentSignatureRequestsQuery,
        ApiResponse<List<CustomerSignatureRequestDto>>
    >
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetUrgentSignatureRequestsHandler> _logger;

    public GetUrgentSignatureRequestsHandler(
        SignatureDbContext db,
        ILogger<GetUrgentSignatureRequestsHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CustomerSignatureRequestDto>>> Handle(
        GetUrgentSignatureRequestsQuery request,
        CancellationToken ct
    )
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo solicitudes urgentes para cliente {CustomerId}",
                request.CustomerId
            );

            // Solicitudes urgentes: más de 2 días pendientes
            var urgentDate = DateTime.UtcNow.AddDays(-2);

            var requests = await (
                from r in _db.SignatureRequests
                join s in _db.Signers on r.Id equals s.SignatureRequestId
                where
                    s.CustomerId == request.CustomerId
                    && (
                        r.Status == SignatureStatus.Pending
                        || r.Status == SignatureStatus.InProgress
                    )
                    && r.CreatedAt < urgentDate
                group new { r, s } by new
                {
                    r.Id,
                    r.DocumentId,
                    r.Status,
                    r.CreatedAt,
                    r.UpdatedAt,
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
                    TotalSigners = g.Count(),
                    CompletedSigners = g.Count(x => x.s.Status == SignerStatus.Signed),
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
                            CreatedAt = x.s.CreatedAt,
                        })
                        .ToList(),
                }
            ).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

            _logger.LogInformation(
                "Encontradas {Count} solicitudes urgentes para cliente {CustomerId}",
                requests.Count,
                request.CustomerId
            );

            return ApiResponse<List<CustomerSignatureRequestDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo solicitudes urgentes para cliente {CustomerId}",
                request.CustomerId
            );
            return ApiResponse<List<CustomerSignatureRequestDto>>.Fail(
                "Error interno del servidor"
            );
        }
    }
}
