using Application.DTOS;
using MediatR;

namespace Infrastructure.Queries;

public record class GetStatementQuery(Guid StatementId) : IRequest<StatementDto>;
