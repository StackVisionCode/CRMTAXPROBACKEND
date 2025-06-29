namespace Application.Common.DTO;

public record EmailNotificationDto(
    string Template, // "Auth/Login.html"
    object Model, // datos anónimos o DTO concreto
    string Subject,
    string To,
    Guid? CompanyId = null,
    Guid? UserId = null,
    string? InlineLogoPath = null // opcional
);
