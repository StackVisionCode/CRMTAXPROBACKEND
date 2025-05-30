using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using MediatR;

namespace CustomerService.Queries.TaxInformationQueries;

public record class GetByIdTaxInformationQueries(Guid Id) : IRequest<ApiResponse<ReadTaxInformationDTO>>;