using Common;
using DTOs.FirmsDto;
using MediatR;

namespace Commands.Firms;

    public record CreateFirmCommand(CreateFirmDto Firm) : IRequest<ApiResponse<bool>>;
