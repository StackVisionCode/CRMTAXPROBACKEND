namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp()
    {
        Server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "TIC-LAP-09\\SQLEXPRESS";

        User = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";

        Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Brittany040238.";
    }
}
