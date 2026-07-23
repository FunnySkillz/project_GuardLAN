using GuardLan.Application.Scanning;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class ScanExecutionService(
    IUnitOfWork unitOfWork,
    INetworkScanner networkScanner,
    TimeProvider timeProvider) : IScanExecutionService
{
    public async Task<ScanExecutionResult> ExecuteNextQueuedScanAsync(CancellationToken cancellationToken = default)
    {
        var scanRun = await unitOfWork.NetworkScanRuns.GetNextQueuedAsync(cancellationToken);

        if (scanRun is null)
        {
            return ScanExecutionResult.NoQueuedScan;
        }

        var nowUtc = GetUtcNow();
        scanRun.Status = NetworkScanStatus.Running;
        scanRun.StartedUtc = nowUtc;
        scanRun.Notes = "Scanner worker is running nmap discovery.";
        unitOfWork.NetworkScanRuns.Update(scanRun);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var discoveredDevices = await networkScanner.ScanAsync(scanRun.Subnet, cancellationToken);
            var updateResult = await ApplyScanResultsAsync(scanRun, discoveredDevices, cancellationToken);

            return updateResult;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            scanRun.Status = NetworkScanStatus.Failed;
            scanRun.FinishedUtc = GetUtcNow();
            scanRun.Notes = exception.Message;
            unitOfWork.NetworkScanRuns.Update(scanRun);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ScanExecutionResult(
                true,
                scanRun.Id,
                0,
                0,
                0,
                $"Scan failed: {exception.Message}");
        }
    }

    private async Task<ScanExecutionResult> ApplyScanResultsAsync(
        NetworkScanRun scanRun,
        IReadOnlyList<DiscoveredNetworkDevice> discoveredDevices,
        CancellationToken cancellationToken)
    {
        var nowUtc = GetUtcNow();
        var trackedDevices = await unitOfWork.Devices.GetDevicesForScanUpdateAsync(cancellationToken);
        var discoveredByMacAddress = discoveredDevices
            .Where(device => !string.IsNullOrWhiteSpace(device.MacAddress))
            .Select(device => device with { MacAddress = NormalizeMacAddress(device.MacAddress!) })
            .GroupBy(device => device.MacAddress!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var trackedByMacAddress = trackedDevices
            .ToDictionary(device => NormalizeMacAddress(device.MacAddress), StringComparer.OrdinalIgnoreCase);

        var newDevices = 0;
        var devicesMarkedOffline = 0;

        foreach (var discoveredDevice in discoveredByMacAddress.Values)
        {
            if (trackedByMacAddress.TryGetValue(discoveredDevice.MacAddress!, out var existingDevice))
            {
                existingDevice.IpAddress = discoveredDevice.IpAddress;
                existingDevice.Hostname = NormalizeNullable(discoveredDevice.Hostname) ?? existingDevice.Hostname;
                existingDevice.Vendor = NormalizeNullable(discoveredDevice.Vendor) ?? existingDevice.Vendor;
                existingDevice.LastSeenUtc = nowUtc;
                existingDevice.IsOnline = true;
                unitOfWork.Devices.Update(existingDevice);
                continue;
            }

            var networkDevice = new NetworkDevice
            {
                Id = Guid.NewGuid(),
                IpAddress = discoveredDevice.IpAddress,
                MacAddress = discoveredDevice.MacAddress!,
                Hostname = NormalizeNullable(discoveredDevice.Hostname),
                Vendor = NormalizeNullable(discoveredDevice.Vendor),
                DeviceType = DeviceType.Unknown,
                IsTrusted = false,
                FirstSeenUtc = nowUtc,
                LastSeenUtc = nowUtc,
                IsOnline = true
            };

            await unitOfWork.Devices.AddAsync(networkDevice, cancellationToken);
            await unitOfWork.SecurityAlerts.AddAsync(
                CreateNewDeviceAlert(networkDevice, nowUtc),
                cancellationToken);

            newDevices++;
        }

        foreach (var trackedDevice in trackedDevices)
        {
            var normalizedMacAddress = NormalizeMacAddress(trackedDevice.MacAddress);

            if (!trackedDevice.IsOnline || discoveredByMacAddress.ContainsKey(normalizedMacAddress))
            {
                continue;
            }

            trackedDevice.IsOnline = false;
            trackedDevice.LastSeenUtc = nowUtc;
            unitOfWork.Devices.Update(trackedDevice);
            await unitOfWork.SecurityAlerts.AddAsync(
                CreateDeviceOfflineAlert(trackedDevice, nowUtc),
                cancellationToken);

            devicesMarkedOffline++;
        }

        scanRun.Status = NetworkScanStatus.Completed;
        scanRun.FinishedUtc = nowUtc;
        scanRun.DevicesDiscovered = discoveredDevices.Count;
        scanRun.Notes = $"Discovered {discoveredDevices.Count} hosts; added {newDevices} new devices.";
        unitOfWork.NetworkScanRuns.Update(scanRun);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ScanExecutionResult(
            true,
            scanRun.Id,
            discoveredDevices.Count,
            newDevices,
            devicesMarkedOffline,
            scanRun.Notes);
    }

    private static SecurityAlert CreateNewDeviceAlert(NetworkDevice device, DateTime nowUtc)
    {
        return new SecurityAlert
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            Severity = AlertSeverity.High,
            Type = "UnknownDeviceConnected",
            Message = $"New unknown device connected at {device.IpAddress}.",
            CreatedUtc = nowUtc
        };
    }

    private static SecurityAlert CreateDeviceOfflineAlert(NetworkDevice device, DateTime nowUtc)
    {
        return new SecurityAlert
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            Severity = AlertSeverity.Low,
            Type = "DeviceDisappeared",
            Message = $"Device {device.Hostname ?? device.IpAddress} disappeared from the network.",
            CreatedUtc = nowUtc
        };
    }

    private DateTime GetUtcNow()
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }

    private static string NormalizeMacAddress(string macAddress)
    {
        return macAddress.Trim().Replace("-", ":").ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
