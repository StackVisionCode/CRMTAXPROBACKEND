using AutoMapper;
using Common;
using CustomerService.DTOs.PreferredContactDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.PreferredContactQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.PreferredContactHandlers;

public class GetByIdPreferredContactHandlers
    : IRequestHandler<GetByIdPreferredContactQueries, ApiResponse<ReadPreferredContactDTO>>
{
    private readonly ILogger<GetByIdPreferredContactHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public GetByIdPreferredContactHandlers(
        ILogger<GetByIdPreferredContactHandlers> logger,
        IMapper mapper,
        ApplicationDbContext context
    )
    {
        _logger = logger;
        _mapper = mapper;
        _context = context;
    }

    public async Task<ApiResponse<ReadPreferredContactDTO>> Handle(
        GetByIdPreferredContactQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _context.PreferredContacts.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );
            if (result is null)
            {
                _logger.LogInformation("No PreferredContact found with id: {Id}", request.Id);
                return new ApiResponse<ReadPreferredContactDTO>(
                    false,
                    "No PreferredContact found",
                    null!
                );
            }
            var PreferredContactDTO = _mapper.Map<ReadPreferredContactDTO>(result);
            _logger.LogInformation(
                "PreferredContact retrieved successfully: {PreferredContact}",
                PreferredContactDTO
            );
            return new ApiResponse<ReadPreferredContactDTO>(
                true,
                "PreferredContact retrieved successfully",
                PreferredContactDTO
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving PreferredContact: {Message}", ex.Message);
            return new ApiResponse<ReadPreferredContactDTO>(false, ex.Message, null!);
        }
    }
}
