using AutoMapper;
using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class GetByIdContactInfoHandler
    : IRequestHandler<GetByIdContactInfoQueries, ApiResponse<ReadContactInfoDTO>>
{
    private readonly ILogger<GetByIdContactInfoHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetByIdContactInfoHandler(
        ILogger<GetByIdContactInfoHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadContactInfoDTO>> Handle(
        GetByIdContactInfoQueries request,
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
                where contactInfo.Id == request.Id
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
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("ContactInfo with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadContactInfoDTO>(false, "ContactInfo not found", null!);
            }

            return new ApiResponse<ReadContactInfoDTO>(
                true,
                "ContactInfo retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting ContactInfo: {Message}", ex.Message);
            return new ApiResponse<ReadContactInfoDTO>(false, ex.Message, null!);
        }
    }
}
