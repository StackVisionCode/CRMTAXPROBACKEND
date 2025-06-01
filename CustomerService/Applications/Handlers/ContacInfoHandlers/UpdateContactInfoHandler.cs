using AutoMapper;
using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class UpdateContactInfoHandler
    : IRequestHandler<UpdateContactInfoCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateContactInfoHandler> _logger;

    public UpdateContactInfoHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateContactInfoHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateContactInfoCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingContactInfo = await _dbContext.ContactInfos.FirstOrDefaultAsync(
                c => c.Id == request.contactInfo.Id,
                cancellationToken
            );

            if (existingContactInfo == null)
            {
                _logger.LogWarning(
                    "ContactInfo with ID {Id} not found for update",
                    request.contactInfo.Id
                );
                return new ApiResponse<bool>(false, "ContactInfo not found", false);
            }

            var duplicateExists = await _dbContext.ContactInfos.AnyAsync(
                c =>
                    c.Email == request.contactInfo.Email.Trim().ToUpperInvariant()
                    && c.CustomerId == request.contactInfo.CustomerId
                    && c.Id != request.contactInfo.Id,
                cancellationToken
            );

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "ContactInfo with Email {Email} already exists",
                    request.contactInfo.Email
                );
                return new ApiResponse<bool>(
                    false,
                    "ContactInfo with this Email already exists",
                    false
                );
            }

            // Map the updated properties
            _mapper.Map(request.contactInfo, existingContactInfo);
            existingContactInfo.UpdatedAt = DateTime.UtcNow;

            _dbContext.ContactInfos.Update(existingContactInfo);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation(
                "ContactInfo updated successfully: {ContactInfo}",
                existingContactInfo
            );
            return new ApiResponse<bool>(result, "ContactInfo updated successfully", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ContactInfo");
            return new ApiResponse<bool>(false, "Error updating ContactInfo", false);
        }
    }
}
