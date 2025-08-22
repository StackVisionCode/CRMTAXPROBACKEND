using Common;
using DTOs.MessageDTOs;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class SendMessageCommand(SendMessageDTO MessageData)
    : IRequest<ApiResponse<MessageDTO>>;
