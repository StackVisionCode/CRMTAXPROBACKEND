using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record UpdateEmailCommand(
    Guid Id,
    UpdateEmailDTO EmailDto,
    Guid CompanyId,
    Guid LastModifiedByTaxUserId
) : IRequest<EmailDTO>;
