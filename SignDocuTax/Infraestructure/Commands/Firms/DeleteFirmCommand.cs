using Common;
using DTOs.FirmsDto;
using MediatR;

namespace Commands.Firms;
public record DeleteFirmCommand(DeleteFirmDto Firm) : IRequest<ApiResponse<bool>>;