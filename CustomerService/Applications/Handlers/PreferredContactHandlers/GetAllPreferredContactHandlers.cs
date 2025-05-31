using AutoMapper;
using Common;
using CustomerService.DTOs.PreferredContactDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.PreferredContactQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.PreferredContactHandlers;

public class GetAllPreferredContactHandlers
    : IRequestHandler<GetAllPreferredContactQueries, ApiResponse<List<ReadPreferredContactDTO>>>
{
    private readonly ILogger<GetAllPreferredContactHandlers> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllPreferredContactHandlers(
        ILogger<GetAllPreferredContactHandlers> logger,
        ApplicationDbContext context,
        IMapper mapper
    )
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ReadPreferredContactDTO>>> Handle(
        GetAllPreferredContactQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _context.PreferredContacts.ToListAsync(cancellationToken);
            if (result is null || !result.Any())
            {
                _logger.LogInformation("No PreferredContacts found");
                return new ApiResponse<List<ReadPreferredContactDTO>>(
                    false,
                    "No PreferredContacts found",
                    null!
                );
            }
            var PreferredContactDTOs = _mapper.Map<List<ReadPreferredContactDTO>>(result);
            _logger.LogInformation(
                "PreferredContacts retrieved successfully. {PreferredContacts}",
                PreferredContactDTOs
            );
            return new ApiResponse<List<ReadPreferredContactDTO>>(
                true,
                "PreferredContacts retrieved successfully.",
                PreferredContactDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving PreferredContacts: {Message}", ex.Message);
            return new ApiResponse<List<ReadPreferredContactDTO>>(false, ex.Message, null!);
        }
    }
}
