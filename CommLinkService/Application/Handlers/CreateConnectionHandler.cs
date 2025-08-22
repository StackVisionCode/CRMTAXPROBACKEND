using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.ConnectionDTOs;
using MediatR;

namespace CommLinkService.Application.Handlers;

public sealed class CreateConnectionHandler
    : IRequestHandler<CreateConnectionCommand, ApiResponse<ConnectionDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateConnectionHandler> _logger;

    public CreateConnectionHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        ILogger<CreateConnectionHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectionDTO>> Handle(
        CreateConnectionCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var connection = new Connection
            {
                Id = Guid.NewGuid(),
                UserType = request.UserType,
                TaxUserId = request.TaxUserId,
                CustomerId = request.CustomerId,
                CompanyId = request.CompanyId,
                ConnectionId = request.ConnectionId,
                ConnectedAt = DateTime.UtcNow,
                UserAgent = request.UserAgent,
                IpAddress = request.IpAddress,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync(cancellationToken);

            var connectionDto = _mapper.Map<ConnectionDTO>(connection);

            _logger.LogInformation(
                "Connection {ConnectionId} created for {UserType} {UserId}",
                request.ConnectionId,
                request.UserType,
                request.UserType == ParticipantType.TaxUser ? request.TaxUserId : request.CustomerId
            );

            return new ApiResponse<ConnectionDTO>(
                true,
                "Connection created successfully",
                connectionDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connection");
            return new ApiResponse<ConnectionDTO>(false, "Failed to create connection");
        }
    }
}
