namespace CommLinkServices.Application.DTOs;

public class EndCallRequestDto
{
    public Guid ConversationId { get; set; }
    public Guid CallId { get; set; }
}
