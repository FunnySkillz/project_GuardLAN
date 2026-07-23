using GuardLan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence;

public sealed class GuardLanDbContext(DbContextOptions<GuardLanDbContext> options) : DbContext(options)
{
    public DbSet<NetworkDevice> Devices => Set<NetworkDevice>();

    public DbSet<NetworkConnection> Connections => Set<NetworkConnection>();

    public DbSet<TlsObservation> TlsObservations => Set<TlsObservation>();

    public DbSet<DnsQuery> DnsQueries => Set<DnsQuery>();

    public DbSet<SecurityAlert> Alerts => Set<SecurityAlert>();

    public DbSet<NetworkScanRun> NetworkScanRuns => Set<NetworkScanRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NetworkDevice>(entity =>
        {
            entity.ToTable("network_devices");
            entity.HasKey(device => device.Id);
            entity.HasIndex(device => device.MacAddress).IsUnique();
            entity.HasIndex(device => device.IpAddress);

            entity.Property(device => device.IpAddress).HasMaxLength(64);
            entity.Property(device => device.MacAddress).HasMaxLength(32);
            entity.Property(device => device.Hostname).HasMaxLength(128);
            entity.Property(device => device.Vendor).HasMaxLength(128);
            entity.Property(device => device.DeviceType).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<NetworkConnection>(entity =>
        {
            entity.ToTable("network_connections");
            entity.HasKey(connection => connection.Id);
            entity.HasIndex(connection => connection.DeviceId);
            entity.HasIndex(connection => new { connection.Source, connection.SourceRecordId });
            entity.HasIndex(connection => connection.DestinationDomain);
            entity.HasIndex(connection => connection.LastSeenUtc);

            entity.Property(connection => connection.Source).HasMaxLength(64);
            entity.Property(connection => connection.SourceRecordId).HasMaxLength(96);
            entity.Property(connection => connection.DestinationIp).HasMaxLength(64);
            entity.Property(connection => connection.DestinationDomain).HasMaxLength(255);
            entity.Property(connection => connection.Protocol).HasMaxLength(32);

            entity
                .HasOne(connection => connection.Device)
                .WithMany(device => device.Connections)
                .HasForeignKey(connection => connection.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TlsObservation>(entity =>
        {
            entity.ToTable("tls_observations");
            entity.HasKey(observation => observation.Id);
            entity.HasIndex(observation => observation.DeviceId);
            entity.HasIndex(observation => observation.ConnectionId);
            entity.HasIndex(observation => new { observation.Source, observation.SourceRecordId });
            entity.HasIndex(observation => observation.ServerName);
            entity.HasIndex(observation => observation.ObservedUtc);

            entity.Property(observation => observation.Source).HasMaxLength(64);
            entity.Property(observation => observation.SourceRecordId).HasMaxLength(96);
            entity.Property(observation => observation.SourceIp).HasMaxLength(64);
            entity.Property(observation => observation.DestinationIp).HasMaxLength(64);
            entity.Property(observation => observation.ServerName).HasMaxLength(255);
            entity.Property(observation => observation.Version).HasMaxLength(64);
            entity.Property(observation => observation.Cipher).HasMaxLength(128);
            entity.Property(observation => observation.Ja3).HasMaxLength(128);
            entity.Property(observation => observation.Ja3s).HasMaxLength(128);
            entity.Property(observation => observation.Alpn).HasMaxLength(128);

            entity
                .HasOne(observation => observation.Device)
                .WithMany(device => device.TlsObservations)
                .HasForeignKey(observation => observation.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(observation => observation.Connection)
                .WithMany(connection => connection.TlsObservations)
                .HasForeignKey(observation => observation.ConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DnsQuery>(entity =>
        {
            entity.ToTable("dns_queries");
            entity.HasKey(query => query.Id);
            entity.HasIndex(query => query.ClientIp);
            entity.HasIndex(query => query.Domain);
            entity.HasIndex(query => query.TimestampUtc);
            entity.HasIndex(query => new { query.ClientIp, query.Domain, query.TimestampUtc }).IsUnique();

            entity.Property(query => query.ClientIp).HasMaxLength(64);
            entity.Property(query => query.Domain).HasMaxLength(255);

            entity
                .HasOne(query => query.Device)
                .WithMany(device => device.DnsQueries)
                .HasForeignKey(query => query.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SecurityAlert>(entity =>
        {
            entity.ToTable("security_alerts");
            entity.HasKey(alert => alert.Id);
            entity.HasIndex(alert => alert.CreatedUtc);
            entity.HasIndex(alert => alert.ResolvedUtc);

            entity.Property(alert => alert.Severity).HasConversion<string>().HasMaxLength(32);
            entity.Property(alert => alert.Type).HasMaxLength(96);
            entity.Property(alert => alert.Message).HasMaxLength(512);

            entity
                .HasOne(alert => alert.Device)
                .WithMany(device => device.Alerts)
                .HasForeignKey(alert => alert.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NetworkScanRun>(entity =>
        {
            entity.ToTable("network_scan_runs");
            entity.HasKey(scanRun => scanRun.Id);
            entity.HasIndex(scanRun => scanRun.RequestedUtc);

            entity.Property(scanRun => scanRun.Subnet).HasMaxLength(64);
            entity.Property(scanRun => scanRun.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(scanRun => scanRun.Notes).HasMaxLength(512);
        });
    }
}
