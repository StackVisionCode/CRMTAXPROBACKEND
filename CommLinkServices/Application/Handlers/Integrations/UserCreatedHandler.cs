using CommLinkServices.Domain;
using CommLinkServices.Infrastructure.Context;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

namespace CommLinkServices.Application.Handlers.Integrations;

public sealed class UserCreatedHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly CommLinkDbContext _db;
    private readonly ILogger<UserCreatedHandler> _log;

    public UserCreatedHandler(CommLinkDbContext db, ILogger<UserCreatedHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task Handle(UserCreatedEvent e)
    {
        if (await _db.UserDirectories.FindAsync(e.UserId) is not null)
            return; // ya existe

        _db.UserDirectories.Add(
            new UserDirectory
            {
                UserId = e.UserId,
                UserType = e.UserType,
                DisplayName = e.DisplayName,
                Email = e.Email,
                IsOnline = false,
            }
        );
        await _db.SaveChangesAsync();
        _log.LogInformation("UserDirectory ➕ {UserId} ({Type})", e.UserId, e.UserType);
    }
}
