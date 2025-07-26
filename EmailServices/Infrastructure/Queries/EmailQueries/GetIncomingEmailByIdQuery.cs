using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetIncomingEmailByIdQuery(Guid Id) : IRequest<IncomingEmailDTO?>;
