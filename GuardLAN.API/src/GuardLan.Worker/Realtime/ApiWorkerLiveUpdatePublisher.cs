using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace GuardLan.Worker.Realtime;

public sealed class ApiWorkerLiveUpdatePublisher(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ApiWorkerLiveUpdatePublisher> logger) : ILiveUpdatePublisher
{
    private const string InternalKeyHeader = "X-GuardLAN-Internal-Key";
    private readonly bool enabled = configuration.GetValue("LiveUpdates:Http:Enabled", true);
    private readonly string endpointUrl = configuration.GetValue(
        "LiveUpdates:Http:EndpointUrl",
        "http://localhost:5232/api/internal/live-updates")!;
    private readonly string internalPublisherKey =
        configuration.GetValue("GuardLanAuth:InternalPublisherKey", string.Empty) ??
        configuration.GetValue("LiveUpdates:Http:InternalPublisherKey", string.Empty) ??
        string.Empty;

    public async Task PublishAsync(LiveUpdateDto update, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(internalPublisherKey))
        {
            logger.LogWarning("GuardLAN live update publishing is enabled, but no internal publisher key is configured.");
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
            {
                Content = JsonContent.Create(update)
            };
            request.Headers.TryAddWithoutValidation(InternalKeyHeader, internalPublisherKey);

            var httpClient = httpClientFactory.CreateClient(nameof(ApiWorkerLiveUpdatePublisher));
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "GuardLAN live update publish returned HTTP {StatusCode} for {UpdateType}.",
                    (int)response.StatusCode,
                    update.Type);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Could not publish GuardLAN live update {UpdateType} to {EndpointUrl}.",
                update.Type,
                endpointUrl);
        }
    }
}
