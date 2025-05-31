using AutoMapper;
using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class CreateContactInfoHandler
    : IRequestHandler<CreateContactInfoCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateContactInfoHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateContactInfoHandler(
        ILogger<CreateContactInfoHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateContactInfoCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var exists = await _dbContext.ContactInfos.AnyAsync(
                c =>
                    c.Email == request.contactInfo.Email
                    && c.CustomerId == request.contactInfo.CustomerId,
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

            var contactInfo = _mapper.Map<Domains.Customers.ContactInfo>(request.contactInfo);
            contactInfo.CreatedAt = DateTime.UtcNow;
            await _dbContext.ContactInfos.AddAsync(contactInfo, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
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
