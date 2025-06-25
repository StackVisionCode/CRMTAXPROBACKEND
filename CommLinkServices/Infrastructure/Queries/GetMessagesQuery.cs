using CommLinkServices.Application.DTOs;
using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Queries;

public record GetMessagesQuery(Guid ConversationId, Guid UserId, DateTime? After)
    : IRequest<ApiResponse<IEnumerable<MessageDto>>>;
