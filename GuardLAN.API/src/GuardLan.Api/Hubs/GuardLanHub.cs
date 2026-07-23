using GuardLan.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace GuardLan.Api.Hubs;

public sealed class GuardLanHub : Hub
{
    public Task PublishLiveUpdate(LiveUpdateDto update)
    {
        return Clients.All.SendAsync(LiveUpdateMethods.ClientUpdate, update);
    }
}
