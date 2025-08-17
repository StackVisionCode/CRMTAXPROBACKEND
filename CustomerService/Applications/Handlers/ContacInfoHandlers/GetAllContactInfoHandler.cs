using AutoMapper;
using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class GetAllContactInfoHandler
    : IRequestHandler<GetAllContactInfoQueries, ApiResponse<List<ReadContactInfoDTO>>>
{
    private readonly ILogger<GetAllContactInfoHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllContactInfoHandler(
        ILogger<GetAllContactInfoHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ReadContactInfoDTO>>> Handle(
        GetAllContactInfoQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from contactInfo in _dbContext.ContactInfos
                join customer in _dbContext.Customers on contactInfo.CustomerId equals customer.Id
                join preferredContact in _dbContext.PreferredContacts
                    on contactInfo.PreferredContactId equals preferredContact.Id
                select new ReadContactInfoDTO
                {
                    Id = contactInfo.Id,
                    CustomerId = contactInfo.CustomerId,
                    Email = contactInfo.Email,
                    IsLoggin = contactInfo.IsLoggin,
                    PhoneNumber = contactInfo.PhoneNumber,
                    PreferredContactId = contactInfo.PreferredContactId,
                    PasswordClient = contactInfo.PasswordClient,
                    Customer = customer.FirstName + " " + customer.LastName,
                    PreferredContact = preferredContact.Name,
                    // Auditoría
                    CreatedAt = contactInfo.CreatedAt,
                    CreatedByTaxUserId = contactInfo.CreatedByTaxUserId,
                    UpdatedAt = contactInfo.UpdatedAt,
                    LastModifiedByTaxUserId = contactInfo.LastModifiedByTaxUserId,
                }
            ).ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No ContactInfo found.");
                return new ApiResponse<List<ReadContactInfoDTO>>(
                    false,
                    "No ContactInfo found",
                    new List<ReadContactInfoDTO>()
                );
            }

            _logger.LogInformation(
                "ContactInfo retrieved successfully: {Count} records",
                result.Count
            );
            return new ApiResponse<List<ReadContactInfoDTO>>(
                true,
                "ContactInfo retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving ContactInfo: {Message}", ex.Message);
            return new ApiResponse<List<ReadContactInfoDTO>>(
                false,
                ex.Message,
                new List<ReadContactInfoDTO>()
            );
        }
    }
}
