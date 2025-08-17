using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailByIdQuery(Guid EmailId, Guid CompanyId) : IRequest<EmailDTO?>;
