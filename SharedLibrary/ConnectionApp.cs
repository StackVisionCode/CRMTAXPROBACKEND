namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp(string server = "DESKTOP-S0SEBP1", string user = "ccastillo", string password = "Murasaki2527@")
    {
        Server = server;
        User = user;
        Password = password;
    }
}
