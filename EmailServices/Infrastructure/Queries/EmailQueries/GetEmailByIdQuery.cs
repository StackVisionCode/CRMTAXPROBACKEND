using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailByIdQuery(Guid EmailId) : IRequest<EmailDTO?>;
