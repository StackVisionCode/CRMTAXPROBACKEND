using AutoMapper;
using Common;
using CustomerService.Commands.PreferredContactCommads;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.PreferredContactHandlers;

public class CreatePreferredContactHandlers
    : IRequestHandler<CreatePreferredContactCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreatePreferredContactHandlers> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreatePreferredContactHandlers(
        ILogger<CreatePreferredContactHandlers> logger,
        ApplicationDbContext context,
        IMapper mapper
    )
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreatePreferredContactCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var preferredContact = _mapper.Map<Domains.Customers.PreferredContact>(
                request.preferredContact
            );
            preferredContact.CreatedAt = DateTime.UtcNow;
            await _context.PreferredContacts.AddAsync(preferredContact, cancellationToken);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation(
                "PreferredContact created successfully: {PreferredContact}",
                preferredContact
            );
            return new ApiResponse<bool>(
                result,
                result
                    ? "PreferredContact created successfully"
                    : "Failed to create PreferredContact",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating PreferredContact: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
