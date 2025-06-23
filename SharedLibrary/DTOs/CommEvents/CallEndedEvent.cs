namespace SharedLibrary.DTOs.CommEvents;

public sealed record CallEndedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid ConversationId,
    Guid CallId,
    Guid EndedById,
    int DurationSeconds
) : IntegrationEvent(Id, OccurredOn);
