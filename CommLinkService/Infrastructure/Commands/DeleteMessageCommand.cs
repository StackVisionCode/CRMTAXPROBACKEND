using Common;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class DeleteMessageCommand(
    Guid MessageId,
    ParticipantType DeleterType,
    Guid? DeleterTaxUserId,
    Guid? DeleterCustomerId
) : IRequest<ApiResponse<bool>>;
