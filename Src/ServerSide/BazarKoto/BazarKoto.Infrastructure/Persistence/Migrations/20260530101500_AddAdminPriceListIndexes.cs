using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BazarKoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPriceListIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Markets_MarketName",
                table: "Markets",
                column: "MarketName");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_CreatedAt",
                table: "PriceSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_MarketId_CreatedAt",
                table: "PriceSubmissions",
                columns: new[] { "MarketId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_ProductId_CreatedAt",
                table: "PriceSubmissions",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSubmissions_Status_CreatedAt",
                table: "PriceSubmissions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_LocalName",
                table: "Products",
                column: "LocalName");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NameBn",
                table: "Products",
                column: "NameBn");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NameEn",
                table: "Products",
                column: "NameEn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Markets_MarketName",
                table: "Markets");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_CreatedAt",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_MarketId_CreatedAt",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_ProductId_CreatedAt",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PriceSubmissions_Status_CreatedAt",
                table: "PriceSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Products_LocalName",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_NameBn",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_NameEn",
                table: "Products");
        }
    }
}
