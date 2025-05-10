using Common;
using Dtos.FirmsStatusDto;
using MediatR;
namespace Commands.FirmStatus;

public record DeleteFirmStatusCommand(DeleteFirmStatusDto FirmStatusDto) : IRequest<ApiResponse<bool>>;