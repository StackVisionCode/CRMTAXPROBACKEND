using Common;
using Dtos.FirmsStatusDto;
using MediatR;

namespace Queries.FirmStatus;

public record GetAllFirmStatusQuery() : IRequest<ApiResponse<IEnumerable<FirmStatusDto>>>;

