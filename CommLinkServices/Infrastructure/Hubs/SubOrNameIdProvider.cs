using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CommLinkServices.Infrastructure.Hubs;

public sealed class SubOrNameIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext ctx) =>
        ctx.User?.FindFirst("sub")?.Value ?? ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
