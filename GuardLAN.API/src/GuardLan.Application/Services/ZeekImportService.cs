using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class ZeekImportService(
    IZeekConnectionImportService connectionImportService,
    IZeekDnsImportService dnsImportService,
    IZeekTlsImportService tlsImportService,
    TimeProvider timeProvider) : IZeekImportService
{
    public async Task<ZeekImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var connections = await connectionImportService.ImportRecentAsync(cancellationToken);
        var dns = await dnsImportService.ImportRecentAsync(cancellationToken);
        var tls = await tlsImportService.ImportRecentAsync(cancellationToken);

        var imported = connections.Imported + dns.Imported + tls.Imported;

        return new ZeekImportResultDto(
            "Zeek",
            importedAtUtc,
            connections,
            dns,
            tls,
            $"Imported {imported} Zeek records across connection, DNS and TLS logs.");
    }
}
