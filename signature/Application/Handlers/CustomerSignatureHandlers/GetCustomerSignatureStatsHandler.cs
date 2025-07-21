using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastructure.Queries.CustomerSignatureQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler para obtener estadísticas de un cliente
/// </summary>
public class GetCustomerSignatureStatsHandler
    : IRequestHandler<GetCustomerSignatureStatsQuery, ApiResponse<CustomerSignatureStatsDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetCustomerSignatureStatsHandler> _logger;

    public GetCustomerSignatureStatsHandler(
        SignatureDbContext db,
        ILogger<GetCustomerSignatureStatsHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomerSignatureStatsDto>> Handle(
        GetCustomerSignatureStatsQuery request,
        CancellationToken ct
    )
    {
        try
        {
            _logger.LogInformation(
                "Calculando estadísticas para cliente {CustomerId}",
                request.CustomerId
            );

            var stats = await (
                from r in _db.SignatureRequests
                join s in _db.Signers on r.Id equals s.SignatureRequestId
                where s.CustomerId == request.CustomerId
                group new { r, s } by s.CustomerId into g
                select new
                {
                    TotalRequests = g.Select(x => x.r.Id).Distinct().Count(),
                    PendingRequests = g.Where(x =>
                            x.r.Status == SignatureStatus.Pending
                            || x.r.Status == SignatureStatus.InProgress
                        )
                        .Select(x => x.r.Id)
                        .Distinct()
                        .Count(),
                    CompletedRequests = g.Where(x => x.r.Status == SignatureStatus.Completed)
                        .Select(x => x.r.Id)
                        .Distinct()
                        .Count(),
                    RejectedRequests = g.Where(x => x.r.Status == SignatureStatus.Rejected)
                        .Select(x => x.r.Id)
                        .Distinct()
                        .Count(),
                    ExpiredRequests = g.Where(x => x.r.Status == SignatureStatus.Expired)
                        .Select(x => x.r.Id)
                        .Distinct()
                        .Count(),
                    TotalSigners = g.Count(),
                    ActiveSigners = g.Select(x => x.s.Email).Distinct().Count(),
                    LastActivity = g.Max(x => x.r.UpdatedAt ?? x.r.CreatedAt),
                    CompletedSignersWithTime = g.Where(x =>
                        x.s.Status == SignerStatus.Signed && x.s.SignedAtUtc.HasValue
                    ),
                }
            ).FirstOrDefaultAsync(ct);

            if (stats == null)
            {
                return ApiResponse<CustomerSignatureStatsDto>.Ok(
                    new CustomerSignatureStatsDto
                    {
                        CustomerId = request.CustomerId,
                        CustomerName = "Cliente",
                    }
                );
            }

            // Calcular tiempo promedio de finalización
            var avgCompletionTime = 0.0;
            if (stats.CompletedRequests > 0)
            {
                var completedRequestsTime = await (
                    from r in _db.SignatureRequests
                    join s in _db.Signers on r.Id equals s.SignatureRequestId
                    where
                        s.CustomerId == request.CustomerId
                        && r.Status == SignatureStatus.Completed
                        && r.UpdatedAt.HasValue
                    select new { r.CreatedAt, r.UpdatedAt }
                ).ToListAsync(ct);

                if (completedRequestsTime.Any())
                {
                    avgCompletionTime = completedRequestsTime.Average(x =>
                        (x.UpdatedAt!.Value - x.CreatedAt).TotalHours
                    );
                }
            }

            // Calcular solicitudes urgentes (más de 2 días pendientes)
            var urgentCount = await (
                from r in _db.SignatureRequests
                join s in _db.Signers on r.Id equals s.SignatureRequestId
                where
                    s.CustomerId == request.CustomerId
                    && (
                        r.Status == SignatureStatus.Pending
                        || r.Status == SignatureStatus.InProgress
                    )
                    && r.CreatedAt < DateTime.UtcNow.AddDays(-2)
                select r.Id
            )
                .Distinct()
                .CountAsync(ct);

            var result = new CustomerSignatureStatsDto
            {
                CustomerId = request.CustomerId,
                CustomerName = "Cliente", // Obtener del servicio de customers
                TotalRequests = stats.TotalRequests,
                PendingRequests = stats.PendingRequests,
                CompletedRequests = stats.CompletedRequests,
                RejectedRequests = stats.RejectedRequests,
                ExpiredRequests = stats.ExpiredRequests,
                CompletionRate =
                    stats.TotalRequests > 0
                        ? (stats.CompletedRequests * 100.0) / stats.TotalRequests
                        : 0,
                AvgCompletionTimeHours = avgCompletionTime,
                TotalSigners = stats.TotalSigners,
                ActiveSigners = stats.ActiveSigners,
                UrgentRequests = urgentCount,
                LastActivity = stats.LastActivity,
                MonthlyTrend = new List<MonthlyTrendDto>(), // Implementar si es necesario
            };

            _logger.LogInformation(
                "Estadísticas calculadas para cliente {CustomerId}: {TotalRequests} solicitudes",
                request.CustomerId,
                result.TotalRequests
            );

            return ApiResponse<CustomerSignatureStatsDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calculando estadísticas para cliente {CustomerId}",
                request.CustomerId
            );
            return ApiResponse<CustomerSignatureStatsDto>.Fail("Error interno del servidor");
        }
    }
}
