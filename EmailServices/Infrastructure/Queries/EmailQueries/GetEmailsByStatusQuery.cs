using Application.Common.DTO;
using EmailServices.Domain;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsByStatusQuery(EmailStatus Status, Guid? UserId = null)
    : IRequest<IEnumerable<EmailDTO>>;
