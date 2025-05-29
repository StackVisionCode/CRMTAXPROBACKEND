using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

public sealed class UserLoginEventHandler : IIntegrationEventHandler<UserLoginEvent>
{

 private readonly ILogger<UserLoginEventHandler> _log;

    public UserLoginEventHandler(ILogger<UserLoginEventHandler> log)
    {
        _log = log;
    }
    
    public Task Handle(UserLoginEvent @event)
    {
        
        throw new NotImplementedException();
    }
}