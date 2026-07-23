using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface ILiveUpdatePublisher
{
    Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default);
}
