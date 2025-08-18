using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record UpdateEmailConfigCommand(
    Guid Id,
    UpdateEmailConfigDTO Config,
    Guid CompanyId,
    Guid LastModifiedByTaxUserId
) : IRequest<EmailConfigDTO>;
