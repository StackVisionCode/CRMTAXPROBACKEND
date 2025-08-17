using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record CreateEmailConfigCommand(
    CreateEmailConfigDTO Config,
    Guid CompanyId,
    Guid CreatedByTaxUserId
) : IRequest<EmailConfigDTO>;
