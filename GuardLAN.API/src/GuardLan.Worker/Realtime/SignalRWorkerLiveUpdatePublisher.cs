using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace GuardLan.Worker.Realtime;

public sealed class SignalRWorkerLiveUpdatePublisher(
    IConfiguration configuration,
    ILogger<SignalRWorkerLiveUpdatePublisher> logger)
    : ILiveUpdatePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly bool enabled = configuration.GetValue("LiveUpdates:SignalR:Enabled", true);
    private readonly string hubUrl =
        configuration.GetValue("LiveUpdates:SignalR:HubUrl", "http://localhost:5232/hubs/guardlan")!;
    private HubConnection? connection;

    public async Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return;
        }

        try
        {
            var hubConnection = await GetStartedConnectionAsync(cancellationToken);

            await hubConnection.InvokeAsync(
                LiveUpdateMethods.ServerPublish,
                update,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not publish GuardLAN live update {UpdateType} to {HubUrl}.",
                update.Type,
                hubUrl);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        connectionLock.Dispose();
    }

    private async Task<HubConnection> GetStartedConnectionAsync(CancellationToken cancellationToken)
    {
        await connectionLock.WaitAsync(cancellationToken);

        try
        {
            connection ??= new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            if (connection.State == HubConnectionState.Disconnected)
            {
                await connection.StartAsync(cancellationToken);
            }

            return connection;
        }
        finally
        {
            connectionLock.Release();
        }
    }
}
