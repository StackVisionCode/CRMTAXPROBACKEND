using Common;
using DTOs.FirmsDto;
using MediatR;
namespace Queries.Firms;
 public record GetAllFirmsQuery() : IRequest<ApiResponse<List<ReadFirmDto>>>;