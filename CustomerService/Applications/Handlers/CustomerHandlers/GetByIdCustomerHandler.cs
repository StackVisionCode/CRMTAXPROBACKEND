using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetByIdCustomerHanlder : IRequestHandler<GetByIdCustomerQueries, ApiResponse<ReadCustomerDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetByIdCustomerHanlder> _logger;

    public GetByIdCustomerHanlder(ApplicationDbContext context, IMapper mapper, ILogger<GetByIdCustomerHanlder> logger)
    {

        _dbContext = context;
        _mapper = mapper;
        _logger = logger;
    }


    public async Task<ApiResponse<ReadCustomerDTO>> Handle(GetByIdCustomerQueries request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            if (customer == null)
            {
                _logger.LogWarning("Customers with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadCustomerDTO>(false, "Customers not found", null!);
            }

            var dtos = _mapper.Map<ReadCustomerDTO>(customer);
            return new ApiResponse<ReadCustomerDTO>(true, "Customer retrieved successfully", dtos);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting document: {Message}", e.Message);
            return new ApiResponse<ReadCustomerDTO>(false, e.Message, null!);
        }



    }
}