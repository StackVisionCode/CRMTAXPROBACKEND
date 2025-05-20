namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

<<<<<<< HEAD
    public ConnectionApp(string server = "localhost\\SQLEXPRESS", string user = "jp", string password = "Jp1212@11")
=======
    public ConnectionApp()
>>>>>>> 4a3bb8af076a518c3ba1701db66c0b1b83ba6579
    {
        Server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "DESKTOP-S0SEBP1";

        User = Environment.GetEnvironmentVariable("DB_USER") ?? "ccastillo";

        Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourPassword";
    }
}
