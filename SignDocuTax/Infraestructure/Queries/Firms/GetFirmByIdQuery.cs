using Common;
using DTOs.FirmsDto;
using MediatR;
namespace Queries.Firms;
 public record GetFirmByIdQuery(int Id) : IRequest<ApiResponse<ReadFirmDto>>;