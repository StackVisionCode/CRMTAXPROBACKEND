using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using MediatR;

namespace CustomerService.Queries.TaxInformationQueries;

public record class GetAllTaxInformationQueries
    : IRequest<ApiResponse<List<ReadTaxInformationDTO>>>;
