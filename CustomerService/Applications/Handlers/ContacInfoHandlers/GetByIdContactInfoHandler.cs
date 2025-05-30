using AutoMapper;
using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class GetByIdContactInfoHandler : IRequestHandler<GetByIdContactInfoQueries, ApiResponse<ReadContactInfoDTO>>
{
  private readonly ILogger<GetByIdAddressHandler> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  public GetByIdContactInfoHandler(ILogger<GetByIdAddressHandler> logger, ApplicationDbContext dbContext, IMapper mapper)
  {
    _logger = logger;
    _dbContext = dbContext;
    _mapper = mapper;
  }
  public async Task<ApiResponse<ReadContactInfoDTO>> Handle(GetByIdContactInfoQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var result = await (
        from contactInfo in _dbContext.ContactInfos
        join customer in _dbContext.Customers on contactInfo.CustomerId equals customer.Id
        join preferredContact in _dbContext.PreferredContacts on contactInfo.PreferredContactId equals preferredContact.Id
        select new ReadContactInfoDTO
        {
          Id = contactInfo.Id,
          Email = contactInfo.Email,
          IsLoggin = contactInfo.IsLoggin,
          PhoneNumber = contactInfo.PhoneNumber,
          Customer = customer.FirstName + " " + customer.LastName,
          PreferredContact = preferredContact.Name
        }
      ).FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

      if (result == null)
      {
        _logger.LogWarning("Address with ID {Id} not found.", request.Id);
        return new ApiResponse<ReadContactInfoDTO>(false, "Address not found", null!);
      }
      var contactInfoDTO = _mapper.Map<ReadContactInfoDTO>(result);
      return new ApiResponse<ReadContactInfoDTO>(true, "Address retrieved successfully", contactInfoDTO);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error getting address: {Message}", ex.Message);
      return new ApiResponse<ReadContactInfoDTO>(false, ex.Message, null!);
    }
  }
}