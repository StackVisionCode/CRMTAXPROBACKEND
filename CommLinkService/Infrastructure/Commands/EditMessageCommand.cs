using Common;
using DTOs.MessageDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class EditMessageCommand(
    Guid MessageId,
    ParticipantType EditorType,
    Guid? EditorTaxUserId,
    Guid? EditorCustomerId,
    string NewContent
) : IRequest<ApiResponse<MessageDTO>>;
