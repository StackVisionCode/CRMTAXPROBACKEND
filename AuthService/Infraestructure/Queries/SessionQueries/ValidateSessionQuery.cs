using MediatR;

namespace Queries.SessionQueries;

public record ValidateSessionQuery(string SessionUid) : IRequest<bool>;