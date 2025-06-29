using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailConfigByIdQuery(Guid Id) : IRequest<EmailConfigDTO?>;
