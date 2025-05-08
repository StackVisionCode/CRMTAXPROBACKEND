namespace CustomerService.Infrastructure.Configuration;

public class RabbitSettings
{
    public string Host { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;

    public const string AuthExchange          = "auth.events";
    public const string LoginEventRoutingKey  = "auth.login";
    public const string LoginEventsQueue      = "auth.login.customer";
}