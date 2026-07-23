using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class NoOpLiveUpdatePublisher : ILiveUpdatePublisher
{
    public Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
