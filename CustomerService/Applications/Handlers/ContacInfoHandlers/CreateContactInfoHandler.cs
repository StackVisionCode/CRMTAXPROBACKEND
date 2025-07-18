using AutoMapper;
using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Domains.Customers;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class CreateContactInfoHandler
    : IRequestHandler<CreateContactInfoCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateContactInfoHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;

    public CreateContactInfoHandler(
        ILogger<CreateContactInfoHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper,
        IEventBus eventBus
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateContactInfoCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var emailNorm = request.contactInfo.Email.Trim().ToUpperInvariant();

            var exists = await _dbContext.ContactInfos.AnyAsync(
                c => c.Email == emailNorm && c.CustomerId == request.contactInfo.CustomerId,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "ContactInfo already exists with Email: {Email}",
                    request.contactInfo.Email
                );
                return new ApiResponse<bool>(
                    false,
                    "ContactInfo with this Email already exists.",
                    false
                );
            }

            var contactInfo = _mapper.Map<ContactInfo>(request.contactInfo);
            contactInfo.IsLoggin = false;
            contactInfo.PasswordClient = null;
            contactInfo.CreatedAt = DateTime.UtcNow;
            await _dbContext.ContactInfos.AddAsync(contactInfo, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                var customer = await _dbContext.Customers.FirstOrDefaultAsync(
                    c => c.Id == request.contactInfo.CustomerId,
                    cancellationToken
                );

                _logger.LogInformation(
                    "UserCreatedEvent published for Customer {Id}",
                    customer?.Id
                );
            }

            _logger.LogInformation("ContactInfo created successfully: {ContactInfo}", contactInfo);
            return new ApiResponse<bool>(
                result,
                result ? "ContactInfo created successfully" : "Failed to create ContactInfo",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating ContactInfo: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
