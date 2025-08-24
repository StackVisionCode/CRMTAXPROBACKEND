using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetCompanyAllSessionsHandler
    : IRequestHandler<GetCompanyAllSessionsQuery, ApiResponse<List<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyAllSessionsHandler> _logger;

    public GetCompanyAllSessionsHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetCompanyAllSessionsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SessionDTO>>> Handle(
        GetCompanyAllSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Obtener la empresa del usuario solicitante
            var requestingUserCompanyQuery =
                from u in _context.TaxUsers
                where u.Id == request.RequestingUserId && u.IsActive
                select u.CompanyId;

            var requestingUserCompanyId = await requestingUserCompanyQuery.FirstOrDefaultAsync(
                cancellationToken
            );

            if (requestingUserCompanyId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Requesting user not found or inactive: {UserId}",
                    request.RequestingUserId
                );
                return new ApiResponse<List<SessionDTO>>(
                    false,
                    "User not found or inactive",
                    new List<SessionDTO>()
                );
            }

            // 2. Obtener todas las sesiones de usuarios de la empresa (últimas 30 días para performance)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var companySessionsQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where u.CompanyId == requestingUserCompanyId && s.CreatedAt >= thirtyDaysAgo
                orderby s.CreatedAt descending
                select new SessionDTO
                {
                    Id = s.Id,
                    TaxUserId = s.TaxUserId,
                    TokenRequest = "***HIDDEN***",
                    ExpireTokenRequest = s.ExpireTokenRequest,
                    TokenRefresh = s.TokenRefresh != null ? "***HIDDEN***" : null,
                    IpAddress = s.IpAddress,
                    Location =
                        s.Latitude != null && s.Longitude != null
                            ? $"{s.Latitude},{s.Longitude}"
                            : null,
                    Device = s.Device,
                    IsRevoke = s.IsRevoke,
                };

            var companySessions = await companySessionsQuery
                .Take(500) // Limitar para performance
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} company sessions for user {UserId} company {CompanyId}",
                companySessions.Count,
                request.RequestingUserId,
                requestingUserCompanyId
            );

            return new ApiResponse<List<SessionDTO>>(
                true,
                "Company sessions retrieved successfully",
                companySessions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company sessions for user {UserId}",
                request.RequestingUserId
            );
            return new ApiResponse<List<SessionDTO>>(
                false,
                "An error occurred while retrieving company sessions",
                new List<SessionDTO>()
            );
        }
    }
}
