using Common;
using Dtos.FirmsStatusDto;
using MediatR;


namespace Commands.FirmStatus;

public record UpdateFirmStatusCommand(UpdateFirmStatusDto FirmStatus) : IRequest<ApiResponse<bool>>;
