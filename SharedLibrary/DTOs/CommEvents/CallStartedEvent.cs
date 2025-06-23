namespace SharedLibrary.DTOs.CommEvents;

public sealed record CallStartedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid ConversationId,
    Guid CallId,
    Guid StarterId,
    string CallType // "Voice" | "Video"
) : IntegrationEvent(Id, OccurredOn);
