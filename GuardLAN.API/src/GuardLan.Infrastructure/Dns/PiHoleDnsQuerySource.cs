using GuardLan.Application.Dns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GuardLan.Infrastructure.Dns;

public sealed class PiHoleDnsQuerySource : IDnsQuerySource
{
    private const string AuthPath = "/api/auth";
    private static readonly Regex Ipv4AddressPattern =
        new(@"(?<!\d)(?:\d{1,3}\.){3}\d{1,3}(?!\d)", RegexOptions.Compiled);

    private readonly PiHoleOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PiHoleDnsQuerySource> logger;

    public PiHoleDnsQuerySource(
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<PiHoleDnsQuerySource> logger)
    {
        options = PiHoleOptions.FromConfiguration(configuration);
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public string SourceName => "Pi-hole";

    public bool IsEnabled => options.Enabled && TryBuildBaseUri(out _);

    public async Task<IReadOnlyList<DnsIngestionRecord>> GetRecentQueriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryBuildBaseUri(out var baseUri))
        {
            return [];
        }

        using var httpClient = CreateHttpClient(baseUri);
        var session = await AuthenticateAsync(httpClient, cancellationToken);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildQueriesPath());
            AddSessionHeaders(request, session);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            return ParseQueries(document.RootElement);
        }
        finally
        {
            await LogoutAsync(httpClient, session, cancellationToken);
        }
    }

    private HttpClient CreateHttpClient(Uri baseUri)
    {
        var handler = new HttpClientHandler();
        if (!options.VerifyTls)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return new HttpClient(handler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private async Task<PiHoleSession?> AuthenticateAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApplicationPassword))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, AuthPath)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { password = options.ApplicationPassword }),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        var sessionElement = TryGetProperty(root, out var session, "session") ? session : root;
        var sid = GetStringProperty(sessionElement, "sid", "id", "sessionId", "session_id");
        var csrf = GetStringProperty(sessionElement, "csrf", "csrfToken", "csrf_token");

        return string.IsNullOrWhiteSpace(sid)
            ? null
            : new PiHoleSession(sid, csrf);
    }

    private async Task LogoutAsync(
        HttpClient httpClient,
        PiHoleSession? session,
        CancellationToken cancellationToken)
    {
        if (session is null)
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, AuthPath);
            AddSessionHeaders(request, session);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug(
                    "Pi-hole logout returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Pi-hole logout failed.");
        }
    }

    private static void AddSessionHeaders(HttpRequestMessage request, PiHoleSession? session)
    {
        if (session is null)
        {
            return;
        }

        request.Headers.TryAddWithoutValidation("X-FTL-SID", session.Sid);

        if (!string.IsNullOrWhiteSpace(session.Csrf))
        {
            request.Headers.TryAddWithoutValidation("X-FTL-CSRF", session.Csrf);
        }
    }

    private string BuildQueriesPath()
    {
        var until = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var lookbackSeconds = (long)TimeSpan
            .FromMinutes(Math.Clamp(options.LookbackMinutes, 1, 1440))
            .TotalSeconds;
        var from = until - lookbackSeconds;
        var maxQueries = Math.Clamp(options.MaxQueries, 1, 10000);
        var queryPath = NormalizePath(options.QueriesPath);

        return $"{queryPath}?from={from}&until={until}&length={maxQueries}";
    }

    private bool TryBuildBaseUri(out Uri baseUri)
    {
        if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var parsedUri) &&
            (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
        {
            baseUri = parsedUri;
            return true;
        }

        baseUri = new Uri("http://localhost");
        return false;
    }

    private static string NormalizePath(string path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? "/api/queries" : path.Trim();

        return normalized.StartsWith("/", StringComparison.Ordinal)
            ? normalized
            : $"/{normalized}";
    }

    private static IReadOnlyList<DnsIngestionRecord> ParseQueries(JsonElement root)
    {
        var records = new List<DnsIngestionRecord>();

        foreach (var queryElement in EnumerateQueryElements(root))
        {
            if (TryParseQuery(queryElement, out var record))
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static IEnumerable<JsonElement> EnumerateQueryElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                yield return item;
            }
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (TryGetProperty(root, out var queries, "queries", "data") &&
            queries.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in queries.EnumerateArray())
            {
                yield return item;
            }
        }
    }

    private static bool TryParseQuery(JsonElement queryElement, out DnsIngestionRecord record)
    {
        return queryElement.ValueKind switch
        {
            JsonValueKind.Object => TryParseObjectQuery(queryElement, out record),
            JsonValueKind.Array => TryParseArrayQuery(queryElement, out record),
            _ => Fail(out record)
        };
    }

    private static bool TryParseObjectQuery(JsonElement queryElement, out DnsIngestionRecord record)
    {
        if (!TryGetTimestampProperty(queryElement, out var timestampUtc) ||
            !TryGetTextProperty(queryElement, out var domain, "domain", "query", "requested_domain") ||
            !TryGetTextProperty(queryElement, out var client, "client", "clientIp", "clientIP", "client_ip", "client_address"))
        {
            return Fail(out record);
        }

        var wasBlocked = TryGetDirectBlockedFlag(queryElement, out var blocked)
            ? blocked
            : TryGetStatusBlockedFlag(queryElement, out var statusBlocked) && statusBlocked;

        record = new DnsIngestionRecord(
            ExtractClientIp(client),
            domain,
            wasBlocked,
            timestampUtc);

        return true;
    }

    private static bool TryParseArrayQuery(JsonElement queryElement, out DnsIngestionRecord record)
    {
        var cells = queryElement.EnumerateArray().ToArray();

        if (cells.Length < 4 ||
            !TryParseTimestamp(cells[0], out var timestampUtc) ||
            !TryGetText(cells[2], out var domain) ||
            !TryGetText(cells[3], out var client))
        {
            return Fail(out record);
        }

        var wasBlocked = cells.Length > 4 && IsBlockedStatus(cells[4]);

        record = new DnsIngestionRecord(
            ExtractClientIp(client),
            domain,
            wasBlocked,
            timestampUtc);

        return true;
    }

    private static bool TryGetTimestampProperty(JsonElement element, out DateTime timestampUtc)
    {
        if (TryGetProperty(
                element,
                out var value,
                "timestamp",
                "time",
                "date",
                "timestampUtc",
                "timestamp_utc"))
        {
            return TryParseTimestamp(value, out timestampUtc);
        }

        timestampUtc = default;
        return false;
    }

    private static bool TryGetDirectBlockedFlag(JsonElement element, out bool wasBlocked)
    {
        if (TryGetProperty(element, out var value, "wasBlocked", "was_blocked", "blocked"))
        {
            return TryParseBoolean(value, out wasBlocked);
        }

        wasBlocked = false;
        return false;
    }

    private static bool TryGetStatusBlockedFlag(JsonElement element, out bool wasBlocked)
    {
        if (TryGetProperty(element, out var status, "status"))
        {
            wasBlocked = IsBlockedStatus(status);
            return true;
        }

        wasBlocked = false;
        return false;
    }

    private static bool TryGetTextProperty(
        JsonElement element,
        out string value,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, out var property, propertyName))
            {
                continue;
            }

            if (TryGetText(property, out value))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetText(JsonElement element, out string value)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            TryGetTextFromObject(element, out value))
        {
            return true;
        }

        value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };

        value = value.Trim();
        return value.Length > 0;
    }

    private static bool TryGetTextFromObject(JsonElement element, out string value)
    {
        return TryGetTextProperty(element, out value, "ip", "address", "addr", "name", "value");
    }

    private static bool TryParseTimestamp(JsonElement element, out DateTime timestampUtc)
    {
        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetDouble(out var numericTimestamp))
        {
            timestampUtc = numericTimestamp > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(numericTimestamp)).UtcDateTime
                : DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(numericTimestamp)).UtcDateTime;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var rawTimestamp = element.GetString();
            if (long.TryParse(rawTimestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTimestamp))
            {
                timestampUtc = unixTimestamp > 10_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime
                    : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
                return true;
            }

            if (DateTimeOffset.TryParse(
                    rawTimestamp,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedTimestamp))
            {
                timestampUtc = parsedTimestamp.UtcDateTime;
                return true;
            }
        }

        timestampUtc = default;
        return false;
    }

    private static bool TryParseBoolean(JsonElement element, out bool value)
    {
        value = element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt32(out var number) && number != 0,
            JsonValueKind.String => ParseBooleanText(element.GetString()),
            _ => false
        };

        return element.ValueKind is
            JsonValueKind.True or
            JsonValueKind.False or
            JsonValueKind.Number or
            JsonValueKind.String;
    }

    private static bool ParseBooleanText(string? text)
    {
        if (bool.TryParse(text, out var parsed))
        {
            return parsed;
        }

        var normalized = text?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized.Contains("block", StringComparison.Ordinal) ||
               normalized.Contains("deny", StringComparison.Ordinal) ||
               normalized.Contains("gravity", StringComparison.Ordinal) ||
               normalized.Contains("blacklist", StringComparison.Ordinal);
    }

    private static bool IsBlockedStatus(JsonElement status)
    {
        if (status.ValueKind == JsonValueKind.Number &&
            status.TryGetInt32(out var numericStatus))
        {
            return numericStatus == 1 || numericStatus >= 4;
        }

        if (!TryGetText(status, out var statusText))
        {
            return false;
        }

        var normalized = statusText.ToLowerInvariant();

        return normalized.Contains("block", StringComparison.Ordinal) ||
               normalized.Contains("deny", StringComparison.Ordinal) ||
               normalized.Contains("gravity", StringComparison.Ordinal) ||
               normalized.Contains("regex", StringComparison.Ordinal) ||
               normalized.Contains("blacklist", StringComparison.Ordinal) ||
               normalized.Contains("null", StringComparison.Ordinal) ||
               normalized.Contains("nxra", StringComparison.Ordinal);
    }

    private static string ExtractClientIp(string client)
    {
        var normalized = client.Trim().Trim('[', ']');

        if (IPAddress.TryParse(normalized, out var address))
        {
            return address.ToString();
        }

        var match = Ipv4AddressPattern.Match(normalized);
        if (match.Success && IPAddress.TryParse(match.Value, out var matchedAddress))
        {
            return matchedAddress.ToString();
        }

        return normalized;
    }

    private static string? GetStringProperty(JsonElement element, params string[] propertyNames)
    {
        return TryGetTextProperty(element, out var value, propertyNames) ? value : null;
    }

    private static bool TryGetProperty(
        JsonElement element,
        out JsonElement value,
        params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (propertyNames.Any(name => string.Equals(
                    name,
                    property.Name,
                    StringComparison.OrdinalIgnoreCase)))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool Fail(out DnsIngestionRecord record)
    {
        record = new DnsIngestionRecord(string.Empty, string.Empty, WasBlocked: false, default);
        return false;
    }

    private sealed record PiHoleSession(string Sid, string? Csrf);

    private sealed class PiHoleOptions
    {
        public bool Enabled { get; init; }

        public string BaseUrl { get; init; } = "http://pi.hole";

        public string ApplicationPassword { get; init; } = string.Empty;

        public string QueriesPath { get; init; } = "/api/queries";

        public int LookbackMinutes { get; init; } = 60;

        public int MaxQueries { get; init; } = 1000;

        public bool VerifyTls { get; init; } = true;

        public static PiHoleOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("PiHole");
            var applicationPassword =
                ReadString(section, "ApplicationPassword", string.Empty);

            if (string.IsNullOrWhiteSpace(applicationPassword))
            {
                applicationPassword = ReadString(section, "Password", string.Empty);
            }

            return new PiHoleOptions
            {
                Enabled = ReadBoolean(section, "Enabled", defaultValue: false),
                BaseUrl = ReadString(section, "BaseUrl", "http://pi.hole"),
                ApplicationPassword = applicationPassword,
                QueriesPath = ReadString(section, "QueriesPath", "/api/queries"),
                LookbackMinutes = ReadInteger(section, "LookbackMinutes", 60),
                MaxQueries = ReadInteger(section, "MaxQueries", 1000),
                VerifyTls = ReadBoolean(section, "VerifyTls", defaultValue: true)
            };
        }

        private static string ReadString(IConfiguration section, string key, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(section[key]) ? defaultValue : section[key]!;
        }

        private static int ReadInteger(IConfiguration section, string key, int defaultValue)
        {
            return int.TryParse(section[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }

        private static bool ReadBoolean(IConfiguration section, string key, bool defaultValue)
        {
            return bool.TryParse(section[key], out var value) ? value : defaultValue;
        }
    }
}
