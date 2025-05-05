using AutoMapper;
using AuthService.DTOs.SessionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;
using AuthService.DTOs.PaginationDTO;

namespace Handlers.SessionHandlers;

public class GetAllSessionsHandler : IRequestHandler<GetAllSessionsQuery, ApiResponse<PaginatedResultDTO<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllSessionsHandler> _logger;

    public GetAllSessionsHandler(ApplicationDbContext context, IMapper mapper, ILogger<GetAllSessionsHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PaginatedResultDTO<SessionDTO>>> Handle(GetAllSessionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Setup pagination parameters
            int pageSize = request.PageSize ?? 10;
            int pageNumber = request.PageNumber ?? 1;
            
            // Query with join to get user information
            var query = _context.Sessions
                .Include(s => s.TaxUser)
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt);
            
            // Get total count for pagination
            int totalCount = await query.CountAsync(cancellationToken);
            
            // Apply pagination
            var sessions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            
            // Map to DTOs
            var sessionDtos = _mapper.Map<List<SessionDTO>>(sessions);
            
            // Create paginated result
            var result = new PaginatedResultDTO<SessionDTO>
            {
                Items = sessionDtos,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            
            _logger.LogInformation("Retrieved {Count} sessions. Page {Page} of {TotalPages}", 
                sessionDtos.Count, pageNumber, result.TotalPages);
                
            return new ApiResponse<PaginatedResultDTO<SessionDTO>>(true, "Sessions retrieved successfully", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sessions");
            return new ApiResponse<PaginatedResultDTO<SessionDTO>>(false, "An error occurred while retrieving sessions");
        }
    }
}