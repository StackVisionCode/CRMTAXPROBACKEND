using Common;
using Dtos.FirmsStatusDto;
using MediatR;

namespace Commands.FirmStatus;

public record CreateFirmStatusCommand(CreateFirmStatusDto FirmStatus) : IRequest<ApiResponse<bool>>;
