using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Commands;

public record class EnsureConversationCommand(Guid RequesterId, Guid OtherUserId)
    : IRequest<ApiResponse<Guid>>;
