using CommLinkServices.Application.DTOs;
using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Queries;

public record class GetConversationsQuery(Guid UserId)
    : IRequest<ApiResponse<IEnumerable<ConversationDto>>>;
