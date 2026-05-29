using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BazarKoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessageScreenshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "ContactMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrowserName",
                table: "ContactMessages",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "ContactMessages",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ContactMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OS",
                table: "ContactMessages",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "ContactMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotContentType",
                table: "ContactMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotFileName",
                table: "ContactMessages",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotOriginalFileName",
                table: "ContactMessages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ScreenshotSizeBytes",
                table: "ContactMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotUrl",
                table: "ContactMessages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ContactMessages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "BrowserName",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "OS",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ScreenshotContentType",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ScreenshotFileName",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ScreenshotOriginalFileName",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ScreenshotSizeBytes",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ScreenshotUrl",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ContactMessages");
        }
    }
}
