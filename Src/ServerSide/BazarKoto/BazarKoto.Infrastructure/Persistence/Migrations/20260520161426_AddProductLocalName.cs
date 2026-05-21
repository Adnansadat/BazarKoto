using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BazarKoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLocalName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalName",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalName",
                table: "Products");
        }
    }
}
