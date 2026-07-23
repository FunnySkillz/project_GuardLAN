using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuardLan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertReviewLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "security_alerts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "security_alerts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedUtc",
                table: "security_alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_ReviewedUtc",
                table: "security_alerts",
                column: "ReviewedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_security_alerts_ReviewStatus",
                table: "security_alerts",
                column: "ReviewStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_security_alerts_ReviewedUtc",
                table: "security_alerts");

            migrationBuilder.DropIndex(
                name: "IX_security_alerts_ReviewStatus",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "security_alerts");

            migrationBuilder.DropColumn(
                name: "ReviewedUtc",
                table: "security_alerts");
        }
    }
}
