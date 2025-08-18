using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public sealed class GetOwnCustomersHandler
    : IRequestHandler<GetOwnCustomersQueries, ApiResponse<List<ReadCustomerDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOwnCustomersHandler> _log;

    public GetOwnCustomersHandler(
        ApplicationDbContext db,
        IMapper mapper,
        ILogger<GetOwnCustomersHandler> log
    )
    {
        _db = db;
        _mapper = mapper;
        _log = log;
    }

    public async Task<ApiResponse<List<ReadCustomerDTO>>> Handle(
        GetOwnCustomersQueries request,
        CancellationToken ct
    )
    {
        try
        {
            var query = _db.Customers.Where(c => c.CompanyId == request.CompanyId);

            if (request.CreatedByTaxUserId.HasValue)
            {
                query = query.Where(c => c.CreatedByTaxUserId == request.CreatedByTaxUserId.Value);
            }

            var list = await (
                from cust in query
                join cr in _db.CustomerTypes on cust.CustomerTypeId equals cr.Id
                join occ in _db.Occupations on cust.OccupationId equals occ.Id
                join ms in _db.MaritalStatuses on cust.MaritalStatusId equals ms.Id
                join ci in _db.ContactInfos on cust.Id equals ci.CustomerId into ciGrp
                from ci in ciGrp.DefaultIfEmpty()

                select new ReadCustomerDTO
                {
                    Id = cust.Id,
                    CompanyId = cust.CompanyId,
                    CustomerType = cr.Name,
                    CustomerTypeDescription = cr.Description,
                    FirstName = cust.FirstName,
                    LastName = cust.LastName,
                    MiddleName = cust.MiddleName,
                    DateOfBirth = cust.DateOfBirth,
                    SsnOrItin = cust.SsnOrItin,
                    IsActive = cust.IsActive,
                    IsLogin = ci != null && ci.IsLoggin,
                    Occupation = occ.Name,
                    MaritalStatus = ms.Name,
                    // Auditoría
                    CreatedAt = cust.CreatedAt,
                    CreatedByTaxUserId = cust.CreatedByTaxUserId,
                    UpdatedAt = cust.UpdatedAt,
                    LastModifiedByTaxUserId = cust.LastModifiedByTaxUserId,
                }
            ).ToListAsync(ct);

            if (list is null || !list.Any())
            {
                var message = request.CreatedByTaxUserId.HasValue
                    ? $"No customers found created by TaxUser {request.CreatedByTaxUserId} in Company {request.CompanyId}"
                    : $"No customers found for Company {request.CompanyId}";

                _log.LogInformation(message);
                return new ApiResponse<List<ReadCustomerDTO>>(
                    false,
                    message,
                    new List<ReadCustomerDTO>()
                );
            }

            _log.LogInformation(
                "Customers retrieved successfully for Company {CompanyId}, CreatedBy filter: {CreatedBy}, Count: {Count}",
                request.CompanyId,
                request.CreatedByTaxUserId?.ToString() ?? "All",
                list.Count
            );

            return new ApiResponse<List<ReadCustomerDTO>>(
                true,
                "Customers retrieved successfully",
                list
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error retrieving customers for Company {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<List<ReadCustomerDTO>>(
                false,
                $"Error retrieving customers: {ex.Message}",
                new List<ReadCustomerDTO>()
            );
        }
    }
}
