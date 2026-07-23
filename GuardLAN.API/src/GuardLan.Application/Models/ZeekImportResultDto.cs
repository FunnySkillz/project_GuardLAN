namespace GuardLan.Application.Models;

public sealed record ZeekImportResultDto(
    string Source,
    DateTime ImportedAtUtc,
    ZeekConnectionImportResultDto Connections,
    DnsIngestionResultDto Dns,
    ZeekTlsImportResultDto Tls,
    string Message);
