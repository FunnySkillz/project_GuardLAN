using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuardLan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_health",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StaleAfterUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordsRead = table.Column<int>(type: "integer", nullable: false),
                    RecordsImported = table.Column<int>(type: "integer", nullable: false),
                    RecordsRejected = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_health", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "integration_import_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordsRead = table.Column<int>(type: "integer", nullable: false),
                    RecordsImported = table.Column<int>(type: "integer", nullable: false),
                    RecordsRejected = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_import_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "network_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MacAddress = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Hostname = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Vendor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsTrusted = table.Column<bool>(type: "boolean", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "network_scan_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subnet = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DevicesDiscovered = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_scan_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dns_queries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    WasBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dns_queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dns_queries_network_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "network_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "network_connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceRecordId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    DestinationIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DestinationDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DestinationPort = table.Column<int>(type: "integer", nullable: true),
                    BytesSent = table.Column<long>(type: "bigint", nullable: false),
                    BytesReceived = table.Column<long>(type: "bigint", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_network_connections_network_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "network_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceRecordId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DestinationIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DestinationPort = table.Column<int>(type: "integer", nullable: true),
                    Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvidenceSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_security_alerts_network_connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "network_connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_security_alerts_network_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "network_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tls_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceRecordId = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    SourceIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DestinationIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DestinationPort = table.Column<int>(type: "integer", nullable: true),
                    ServerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Cipher = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Ja3 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Ja3s = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Alpn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WasEstablished = table.Column<bool>(type: "boolean", nullable: true),
                    ObservedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tls_observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tls_observations_network_connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "network_connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tls_observations_network_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "network_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "security_alert_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityAlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_alert_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_security_alert_history_security_alerts_SecurityAlertId",
                        column: x => x.SecurityAlertId,
                        principalTable: "security_alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dns_queries_ClientIp",
                table: "dns_queries",
                column: "ClientIp");

            migrationBuilder.CreateIndex(
                name: "IX_dns_queries_ClientIp_Domain_TimestampUtc",
                table: "dns_queries",
                columns: new[] { "ClientIp", "Domain", "TimestampUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dns_queries_DeviceId",
                table: "dns_queries",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_dns_queries_Domain",
                table: "dns_queries",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_dns_queries_TimestampUtc",
                table: "dns_queries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_integration_health_Kind",
                table: "integration_health",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_integration_health_LastCheckedUtc",
                table: "integration_health",
                column: "LastCheckedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_integration_health_Source",
                table: "integration_health",
                column: "Source",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_health_StaleAfterUtc",
                table: "integration_health",
                column: "StaleAfterUtc");

            migrationBuilder.CreateIndex(
                name: "IX_integration_health_Status",
                table: "integration_health",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_integration_import_runs_CompletedUtc",
                table: "integration_import_runs",
                column: "CompletedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_integration_import_runs_Kind",
                table: "integration_import_runs",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_integration_import_runs_Source",
                table: "integration_import_runs",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_integration_import_runs_Status",
                table: "integration_import_runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_network_connections_DestinationDomain",
                table: "network_connections",
                column: "DestinationDomain");

            migrationBuilder.CreateIndex(
                name: "IX_network_connections_DeviceId",
                table: "network_connections",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_network_connections_LastSeenUtc",
                table: "network_connections",
                column: "LastSeenUtc");

            migrationBuilder.CreateIndex(
                name: "IX_network_connections_Source_SourceRecordId",
                table: "network_connections",
                columns: new[] { "Source", "SourceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_network_devices_IpAddress",
                table: "network_devices",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_network_devices_MacAddress",
                table: "network_devices",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_network_scan_runs_RequestedUtc",
                table: "network_scan_runs",
                column: "RequestedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_security_alert_history_CreatedUtc",
                table: "security_alert_history",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_security_alert_history_SecurityAlertId",
                table: "security_alert_history",
                column: "SecurityAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_ConnectionId",
                table: "security_alerts",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_CreatedUtc",
                table: "security_alerts",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_DeviceId",
                table: "security_alerts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_ResolvedUtc",
                table: "security_alerts",
                column: "ResolvedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_Source_SourceRecordId",
                table: "security_alerts",
                columns: new[] { "Source", "SourceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_tls_observations_ConnectionId",
                table: "tls_observations",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_tls_observations_DeviceId",
                table: "tls_observations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_tls_observations_ObservedUtc",
                table: "tls_observations",
                column: "ObservedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_tls_observations_ServerName",
                table: "tls_observations",
                column: "ServerName");

            migrationBuilder.CreateIndex(
                name: "IX_tls_observations_Source_SourceRecordId",
                table: "tls_observations",
                columns: new[] { "Source", "SourceRecordId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dns_queries");

            migrationBuilder.DropTable(
                name: "integration_health");

            migrationBuilder.DropTable(
                name: "integration_import_runs");

            migrationBuilder.DropTable(
                name: "network_scan_runs");

            migrationBuilder.DropTable(
                name: "security_alert_history");

            migrationBuilder.DropTable(
                name: "tls_observations");

            migrationBuilder.DropTable(
                name: "security_alerts");

            migrationBuilder.DropTable(
                name: "network_connections");

            migrationBuilder.DropTable(
                name: "network_devices");
        }
    }
}
