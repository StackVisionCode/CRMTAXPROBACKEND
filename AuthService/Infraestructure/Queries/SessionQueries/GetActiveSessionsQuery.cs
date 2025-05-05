using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

public record class GetActiveSessionsQuery(int UserId) : IRequest<ApiResponse<List<SessionDTO>>>;