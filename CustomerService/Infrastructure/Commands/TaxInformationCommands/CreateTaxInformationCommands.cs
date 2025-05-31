using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using MediatR;

namespace CustomerService.Commands.TaxInformationCommands;

public record class CreateTaxInformationCommands(CreateTaxInformationDTOs taxInformation) : IRequest<ApiResponse<bool>>;