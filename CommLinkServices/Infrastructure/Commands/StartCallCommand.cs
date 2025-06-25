using CommLinkServices.Application.DTOs;
using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Commands;

public record class StartCallCommand(
    Guid ConversationId,
    Guid StarterId,
    StartCallRequestDto Payload
) : IRequest<ApiResponse<Guid>>;
