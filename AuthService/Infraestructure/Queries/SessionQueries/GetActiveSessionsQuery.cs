using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

public record class GetActiveSessionsQuery(Guid UserId) : IRequest<ApiResponse<List<SessionDTO>>>;