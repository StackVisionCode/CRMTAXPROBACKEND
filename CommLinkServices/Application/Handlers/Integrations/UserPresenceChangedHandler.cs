using CommLinkServices.Infrastructure.Context;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

namespace CommLinkServices.Application.Handlers.Integrations;

public sealed class UserPresenceChangedHandler : IIntegrationEventHandler<UserPresenceChangedEvent>
{
    private readonly CommLinkDbContext _db;
    private readonly ILogger<UserPresenceChangedHandler> _log;

    public UserPresenceChangedHandler(CommLinkDbContext db, ILogger<UserPresenceChangedHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task Handle(UserPresenceChangedEvent e)
    {
        var row = await _db.UserDirectories.FindAsync(e.UserId);
        if (row is null)
            return;

        row.IsOnline = e.IsOnline;
        await _db.SaveChangesAsync();

        _log.LogInformation(
            "Presence {UserId} ➜ {State}",
            e.UserId,
            e.IsOnline ? "online" : "offline"
        );
    }
}
