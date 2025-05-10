using Common;
using Dtos.FirmsStatusDto;
using MediatR;

namespace Queries.FirmStatus;

public record GetFirmStatusByIdQuery(ResponseInfo FirmStatus) : IRequest<ApiResponse<FirmStatusDto>>;
