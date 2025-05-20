namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp()
    {
        Server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "Your Hostname";

        User = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";

        Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourPassword";
    }
}
