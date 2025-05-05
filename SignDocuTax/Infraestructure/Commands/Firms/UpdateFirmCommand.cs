using Common;
using DTOs.FirmsDto;
using MediatR;

namespace Commands.Firms;
 public record UpdateFirmCommand(UpdateFirmDto Firm) : IRequest<ApiResponse<bool>>;