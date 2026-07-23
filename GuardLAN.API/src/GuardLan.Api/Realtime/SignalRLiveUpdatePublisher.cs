using GuardLan.Api.Hubs;
using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace GuardLan.Api.Realtime;

public sealed class SignalRLiveUpdatePublisher(IHubContext<GuardLanHub> hubContext)
    : ILiveUpdatePublisher
{
    public Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.All.SendAsync(
            LiveUpdateMethods.ClientUpdate,
            update,
            cancellationToken);
    }
}
