using Common;
using DTOs.MessageDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class ReactToMessageCommand(
    Guid MessageId,
    ParticipantType ReactorType,
    Guid? ReactorTaxUserId,
    Guid? ReactorCustomerId,
    Guid? ReactorCompanyId,
    string Emoji
) : IRequest<ApiResponse<MessageReactionDTO>>;
