using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BazarKoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrackingDetailsAndPriceLocationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_MarketId",
                table: "PriceSubmissions");

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TrackingGuid",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnionOrWardId",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpazilaId",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserTrackingDetailsId",
                table: "PriceSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserTrackingDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawIpAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RawUserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BrowserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrowserVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GpsLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    GpsLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    GpsAccuracyMeters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GpsPermissionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpBasedCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpBasedRegion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpBasedCity = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpBasedLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IpBasedLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IpLocationProvider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpLocationAccuracy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastKnownDivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastKnownDistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastKnownUpazilaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastKnownUnionOrWardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTrackingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTrackingDetails_Districts_LastKnownDistrictId",
                        column: x => x.LastKnownDistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTrackingDetails_Divisions_LastKnownDivisionId",
                        column: x => x.LastKnownDivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTrackingDetails_UnionOrWards_LastKnownUnionOrWardId",
                        column: x => x.LastKnownUnionOrWardId,
                        principalTable: "UnionOrWards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserTrackingDetails_Upazilas_LastKnownUpazilaId",
                        column: x => x.LastKnownUpazilaId,
                        principalTable: "Upazilas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE ps
                SET
                    ps.DivisionId = m.DivisionId,
                    ps.DistrictId = m.DistrictId,
                    ps.UpazilaId = m.UpazilaId,
                    ps.UnionOrWardId = m.UnionOrWardId
                FROM PriceSubmissions AS ps
                INNER JOIN Markets AS m ON ps.MarketId = m.Id
                WHERE ps.DivisionId IS NULL
                    OR ps.DistrictId IS NULL
                    OR ps.UpazilaId IS NULL
                    OR ps.UnionOrWardId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_DistrictId",
                table: "PriceSubmissions",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_DivisionId",
                table: "PriceSubmissions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_MarketId_ProductId_PriceDate",
                table: "PriceSubmissions",
                columns: new[] { "MarketId", "ProductId", "PriceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_PriceDate",
                table: "PriceSubmissions",
                column: "PriceDate");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_TrackingGuid",
                table: "PriceSubmissions",
                column: "TrackingGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_UnionOrWardId_MarketId_PriceDate",
                table: "PriceSubmissions",
                columns: new[] { "UnionOrWardId", "MarketId", "PriceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_UnionOrWardId_ProductId_PriceDate",
                table: "PriceSubmissions",
                columns: new[] { "UnionOrWardId", "ProductId", "PriceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_UpazilaId",
                table: "PriceSubmissions",
                column: "UpazilaId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_UserTrackingDetailsId",
                table: "PriceSubmissions",
                column: "UserTrackingDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_DeviceType",
                table: "UserTrackingDetails",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_LastKnownDistrictId",
                table: "UserTrackingDetails",
                column: "LastKnownDistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_LastKnownDivisionId",
                table: "UserTrackingDetails",
                column: "LastKnownDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_LastKnownUnionOrWardId",
                table: "UserTrackingDetails",
                column: "LastKnownUnionOrWardId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_LastKnownUpazilaId",
                table: "UserTrackingDetails",
                column: "LastKnownUpazilaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_LastSeenAt",
                table: "UserTrackingDetails",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_OS",
                table: "UserTrackingDetails",
                column: "OS");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrackingDetails_TrackingGuid",
                table: "UserTrackingDetails",
                column: "TrackingGuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceSubmissions_Districts_DistrictId",
                table: "PriceSubmissions",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceSubmissions_Divisions_DivisionId",
                table: "PriceSubmissions",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceSubmissions_UnionOrWards_UnionOrWardId",
                table: "PriceSubmissions",
                column: "UnionOrWardId",
                principalTable: "UnionOrWards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceSubmissions_Upazilas_UpazilaId",
                table: "PriceSubmissions",
                column: "UpazilaId",
                principalTable: "Upazilas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceSubmissions_UserTrackingDetails_UserTrackingDetailsId",
                table: "PriceSubmissions",
                column: "UserTrackingDetailsId",
                principalTable: "UserTrackingDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceSubmissions_Districts_DistrictId",
                table: "PriceSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceSubmissions_Divisions_DivisionId",
                table: "PriceSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceSubmissions_UnionOrWards_UnionOrWardId",
                table: "PriceSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceSubmissions_Upazilas_UpazilaId",
                table: "PriceSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceSubmissions_UserTrackingDetails_UserTrackingDetailsId",
                table: "PriceSubmissions");

            migrationBuilder.DropTable(
                name: "UserTrackingDetails");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_DistrictId",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_DivisionId",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_MarketId_ProductId_PriceDate",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_PriceDate",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_TrackingGuid",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_UnionOrWardId_MarketId_PriceDate",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_UnionOrWardId_ProductId_PriceDate",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_UpazilaId",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_UserTrackingDetailsId",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "TrackingGuid",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "UnionOrWardId",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "UpazilaId",
                table: "PriceSubmissions");

            migrationBuilder.DropColumn(
                name: "UserTrackingDetailsId",
                table: "PriceSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_MarketId",
                table: "PriceSubmissions",
                column: "MarketId");
        }
    }
}
