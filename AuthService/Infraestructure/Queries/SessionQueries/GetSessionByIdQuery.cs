using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

public record class GetSessionByIdQuery(int SessionId) : IRequest<ApiResponse<SessionDTO>>;