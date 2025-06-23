using AuthService.DTOs.SessionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomerQueries;

namespace Handlers.CustomerHandlers;

public class GetCustomerSessionsHandler
    : IRequestHandler<GetCustomerSessionsQuery, ApiResponse<List<ReadCustomerSessionDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetCustomerSessionsHandler> _logger;

    public GetCustomerSessionsHandler(
        ApplicationDbContext db,
        ILogger<GetCustomerSessionsHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReadCustomerSessionDTO>>> Handle(
        GetCustomerSessionsQuery q,
        CancellationToken ct
    )
    {
        try
        {
            var data = await _db
                .CustomerSessions.Where(s => s.CustomerId == q.CustomerId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ReadCustomerSessionDTO
                {
                    SessionId = s.Id,
                    LoginAt = s.CreatedAt,
                    ExpireAt = s.ExpireTokenRequest,
                    Ip = s.IpAddress,
                    Device = s.Device,
                    IsRevoke = s.IsRevoke,
                })
                .ToListAsync(ct);

            _logger.LogInformation("Sesiones obtenidas: {Count}", data.Count);

            return new ApiResponse<List<ReadCustomerSessionDTO>>(true, "Sesiones obtenidas", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesiones: {Message}", ex.Message);
            return new ApiResponse<List<ReadCustomerSessionDTO>>(false, ex.Message, null!);
        }
    }
}
