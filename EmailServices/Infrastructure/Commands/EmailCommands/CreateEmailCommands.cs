using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record class CreateEmailCommand(
    CreateEmailDTO EmailDto,
    Guid CompanyId,
    Guid CreatedByTaxUserId
) : IRequest<EmailDTO>;
