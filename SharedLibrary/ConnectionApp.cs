namespace SharedLibrary;

public class ConnectionApp
{
    public string Server { get; }
    public string User { get; }
    public string Password { get; }

    public ConnectionApp(string server = "localhost\\SQLEXPRESS", string user = "jp", string password = "Jp1212@11")
    {
        Server = server;
        User = user;
        Password = password;
    }
}
