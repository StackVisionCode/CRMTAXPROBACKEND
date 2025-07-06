using AutoMapper;
using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class GetContactInfoByCustomerIdHandler
    : IRequestHandler<GetContactInfoByCustomerIdQueries, ApiResponse<ReadContactInfoDTO>>
{
    private readonly ILogger<GetContactInfoByCustomerIdHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetContactInfoByCustomerIdHandler(
        ILogger<GetContactInfoByCustomerIdHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadContactInfoDTO>> Handle(
        GetContactInfoByCustomerIdQueries request,
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
                where contactInfo.CustomerId == request.CustomerId // Filter by CustomerId
                select new ReadContactInfoDTO
                {
                    Id = contactInfo.Id,
                    Email = contactInfo.Email,
                    IsLoggin = contactInfo.IsLoggin,
                    PhoneNumber = contactInfo.PhoneNumber,
                    Customer = customer.FirstName + " " + customer.LastName,
                    PreferredContact = preferredContact.Name,
                    // Add PasswordClient if needed, but be careful with sensitive data
                    PasswordClient = contactInfo.PasswordClient, // Only if necessary and handled securely
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning(
                    "ContactInfo for Customer ID {CustomerId} not found.",
                    request.CustomerId
                );
                return new ApiResponse<ReadContactInfoDTO>(
                    false,
                    "ContactInfo not found for this customer",
                    null!
                );
            }

            return new ApiResponse<ReadContactInfoDTO>(
                true,
                "ContactInfo retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting ContactInfo for Customer ID {CustomerId}: {Message}",
                request.CustomerId,
                ex.Message
            );
            return new ApiResponse<ReadContactInfoDTO>(false, ex.Message, null!);
        }
    }
}
