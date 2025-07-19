using CommLinkService.Domain.Entities;

namespace CommLinkService.Application.DTOs;

public sealed record RoomInfoDto(
    Guid RoomId,
    string Name,
    RoomType Type,
    DateTime CreatedAt,
    Guid CreatedBy,
    DateTime? LastActivityAt
);
