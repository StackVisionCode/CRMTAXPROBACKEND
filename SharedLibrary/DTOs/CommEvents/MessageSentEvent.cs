namespace SharedLibrary.DTOs.CommEvents;

public sealed record MessageSentEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    bool HasAttachment,
    string? AttachmentUrl
) : IntegrationEvent(Id, OccurredOn);
