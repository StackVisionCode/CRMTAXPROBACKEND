namespace CommLinkServices.Application.DTOs;

public record CallStartedDto(
    Guid Id,
    Guid ConversationId,
    Guid StarterId,
    string Type,
    DateTime StartedAt
);
