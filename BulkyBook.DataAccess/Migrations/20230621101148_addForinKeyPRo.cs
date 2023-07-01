using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addForinKeyPRo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategryId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategryId",
                table: "Products",
                column: "CategryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categries_CategryId",
                table: "Products",
                column: "CategryId",
                principalTable: "Categries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categries_CategryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategryId",
                table: "Products");
        }
    }
}
