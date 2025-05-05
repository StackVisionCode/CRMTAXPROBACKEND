using AuthService.DTOs.UserDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserHandlers;

public class GetTaxUserProfileHandler : IRequestHandler<GetTaxUserProfileQuery, ApiResponse<UserProfileDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTaxUserProfileHandler> _logger;

    public GetTaxUserProfileHandler(ApplicationDbContext db, IMapper mapper, ILogger<GetTaxUserProfileHandler> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDTO>> Handle(GetTaxUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.TaxUsers
                        .Include(u => u.Role)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null)
                return new(false, "User not found");

            var dto = _mapper.Map<UserProfileDTO>(user);
            return new(true, "Ok", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading profile for {UserId}", request.UserId);
            return new(false, "Internal error");
        }
    }
}