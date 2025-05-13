namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp(string server = "", string user = "", string password = "")
    {
        Server = server;
        User = user;
        Password = password;
    }
}

