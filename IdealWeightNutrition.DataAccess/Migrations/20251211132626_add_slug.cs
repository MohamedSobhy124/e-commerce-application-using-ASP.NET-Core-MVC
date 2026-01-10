using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_slug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlugAr",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugEn",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SlugAr",
                table: "Products",
                column: "SlugAr",
                unique: true,
                filter: "[SlugAr] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SlugEn",
                table: "Products",
                column: "SlugEn",
                unique: true,
                filter: "[SlugEn] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SlugAr",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SlugEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SlugAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SlugEn",
                table: "Products");
        }
    }
}
