using Common;
using CustomerService.DTOs.FilingStatusDTOs;
using MediatR;

namespace CustomerService.Commands.FilingStatusCommands;

public record class CreateFilingStatusCommands(CreateFilingStatusDTO filingStatus)
    : IRequest<ApiResponse<bool>>;
