using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

public record class GetSessionByIdQuery(Guid SessionId) : IRequest<ApiResponse<SessionDTO>>;
