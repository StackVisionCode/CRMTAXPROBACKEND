using Common;
using MediatR;

namespace CustomerService.Commands.TaxInformationCommands;

public record class DeleteTaxInformationCommands(Guid Id) : IRequest<ApiResponse<bool>>;
