using MediatR;

namespace Queries.SessionQueries;

public record ValidateSessionQuery(Guid SessionId) : IRequest<bool>;
