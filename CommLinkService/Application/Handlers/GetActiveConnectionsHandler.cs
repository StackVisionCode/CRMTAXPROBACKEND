using AutoMapper;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.ConnectionDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetActiveConnectionsHandler
    : IRequestHandler<GetActiveConnectionsQuery, ApiResponse<List<ConnectionDTO>>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetActiveConnectionsHandler> _logger;

    public GetActiveConnectionsHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        ILogger<GetActiveConnectionsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ConnectionDTO>>> Handle(
        GetActiveConnectionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Obtener conexiones activas según el tipo de usuario
            var connectionsQuery = _context.Connections.Where(c =>
                c.IsActive && c.UserType == request.UserType
            );

            if (request.UserType == ParticipantType.TaxUser)
            {
                connectionsQuery = connectionsQuery.Where(c =>
                    c.TaxUserId == request.TaxUserId
                    && (request.CompanyId == null || c.CompanyId == request.CompanyId)
                );
            }
            else if (request.UserType == ParticipantType.Customer)
            {
                connectionsQuery = connectionsQuery.Where(c => c.CustomerId == request.CustomerId);
            }

            var connections = await connectionsQuery
                .OrderByDescending(c => c.ConnectedAt)
                .ToListAsync(cancellationToken);

            // Mapear a DTOs
            var connectionDtos = _mapper.Map<List<ConnectionDTO>>(connections);

            return new ApiResponse<List<ConnectionDTO>>(
                true,
                "Active connections retrieved successfully",
                connectionDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting active connections for {UserType}",
                request.UserType
            );
            return new ApiResponse<List<ConnectionDTO>>(false, "Failed to get active connections");
        }
    }
}
