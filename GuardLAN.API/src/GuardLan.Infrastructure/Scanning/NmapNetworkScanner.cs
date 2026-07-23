using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GuardLan.Application.Scanning;

namespace GuardLan.Infrastructure.Scanning;

public sealed partial class NmapNetworkScanner : INetworkScanner
{
    public async Task<IReadOnlyList<DiscoveredNetworkDevice>> ScanAsync(
        string subnet,
        CancellationToken cancellationToken = default)
    {
        var output = await RunNmapAsync(subnet, cancellationToken);

        return ParseOutput(output);
    }

    private static async Task<string> RunNmapAsync(string subnet, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "nmap",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-sn");
        startInfo.ArgumentList.Add(subnet);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                "nmap could not be started. Install nmap and ensure it is available on PATH.",
                exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"nmap exited with code {process.ExitCode}."
                    : error.Trim());
        }

        return output;
    }

    private static IReadOnlyList<DiscoveredNetworkDevice> ParseOutput(string output)
    {
        var devices = new List<DiscoveredNetworkDevice>();
        string? currentIpAddress = null;
        string? currentHostname = null;
        string? currentMacAddress = null;
        string? currentVendor = null;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            var reportMatch = ScanReportRegex().Match(line);

            if (reportMatch.Success)
            {
                AddCurrentDevice();

                currentHostname = NormalizeNullable(reportMatch.Groups["host"].Value);
                currentIpAddress = NormalizeNullable(reportMatch.Groups["ip"].Value)
                    ?? NormalizeNullable(reportMatch.Groups["iponly"].Value);
                currentMacAddress = null;
                currentVendor = null;
                continue;
            }

            var macMatch = MacAddressRegex().Match(line);

            if (macMatch.Success)
            {
                currentMacAddress = macMatch.Groups["mac"].Value;
                currentVendor = NormalizeNullable(macMatch.Groups["vendor"].Value);
            }
        }

        AddCurrentDevice();

        return devices;

        void AddCurrentDevice()
        {
            if (string.IsNullOrWhiteSpace(currentIpAddress))
            {
                return;
            }

            devices.Add(new DiscoveredNetworkDevice(
                currentIpAddress,
                currentMacAddress,
                currentHostname,
                currentVendor));
        }
    }

    private static string? NormalizeNullable(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(@"^Nmap scan report for (?:(?<host>.+) \((?<ip>[^)]+)\)|(?<iponly>\S+))$")]
    private static partial Regex ScanReportRegex();

    [GeneratedRegex(@"^MAC Address: (?<mac>[0-9A-Fa-f:]{17})(?: \((?<vendor>.+)\))?$")]
    private static partial Regex MacAddressRegex();
}
