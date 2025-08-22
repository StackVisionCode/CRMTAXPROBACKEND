namespace DTOs.RequestControllerDTOs;

public sealed record EditMessageRequest(string Content);

public sealed record ReactRequest(string Emoji);

public sealed record TypingStatusRequest(bool IsTyping);

public sealed record StartVideoCallRequest(Guid RoomId);

public sealed record EndVideoCallRequest(Guid RoomId, Guid CallId);

public sealed record UpdateStatusRequest(Guid RoomId, bool? IsMuted, bool? IsVideoEnabled);
