namespace SMSServices.Application.DTO;
public class IncomingSmsDto
{
    public string MessageSid { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string NumMedia { get; set; } = string.Empty;
    public DateTime DateReceived { get; set; } = DateTime.UtcNow;
}