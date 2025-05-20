namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp()
    {
        Server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "SQLEXPRESS";

        User = Environment.GetEnvironmentVariable("DB_USER") ?? "jp";

        Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Jp1212@11";
    }
}
