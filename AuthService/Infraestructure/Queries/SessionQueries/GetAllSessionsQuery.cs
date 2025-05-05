using AuthService.DTOs.PaginationDTO;
using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

public record class GetAllSessionsQuery(int? PageSize = null, int? PageNumber = null) : IRequest<ApiResponse<PaginatedResultDTO<SessionDTO>>>;