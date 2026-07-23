using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuardLan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMdacTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mdac_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RegisteredUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mdac_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mdac_sync_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ForegroundSeconds = table.Column<int>(type: "integer", nullable: false),
                    SyncedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mdac_sync_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mdac_registrations_DeviceId",
                table: "mdac_registrations",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mdac_sync_records_DeviceId",
                table: "mdac_sync_records",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_mdac_sync_records_SyncedUtc",
                table: "mdac_sync_records",
                column: "SyncedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mdac_registrations");

            migrationBuilder.DropTable(
                name: "mdac_sync_records");
        }
    }
}
