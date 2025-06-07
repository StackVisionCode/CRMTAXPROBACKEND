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
            // --------- FILTRO (solo clientes del usuario) -----------------------
            var query = _db.Customers.Where(c => c.TaxUserId == request.UserId);

            // --------- PROYECCIÓN ----------------------------------------------
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
                }
            ).ToListAsync(ct);

            if (list is null || !list.Any())
            {
                _log.LogInformation("User {UserId} has no customers", request.UserId);
                return new ApiResponse<List<ReadCustomerDTO>>(
                    false,
                    "No customers found for this user",
                    null!
                );
            }

            var ownCustomerDtos = _mapper.Map<List<ReadCustomerDTO>>(list);
            _log.LogInformation(
                "Customers retrieved successfully for user {UserId}: {Customers}",
                request.UserId,
                ownCustomerDtos
            );
            return new ApiResponse<List<ReadCustomerDTO>>(
                true,
                "Customers retrieved successfully",
                ownCustomerDtos
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving customers for {UserId}", request.UserId);
            return new ApiResponse<List<ReadCustomerDTO>>(
                false,
                $"Error retrieving customers: {ex.Message}",
                null!
            );
        }
    }
}
