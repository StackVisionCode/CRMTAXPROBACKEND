using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using MediatR;

namespace CustomerService.Commands.TaxInformationCommands;

public record class UpdateTaxInformationCommands(UpdateTaxInformationDTOs taxInformation)
    : IRequest<ApiResponse<bool>>;
