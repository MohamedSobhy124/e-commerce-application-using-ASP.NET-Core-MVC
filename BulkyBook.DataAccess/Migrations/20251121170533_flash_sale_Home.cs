using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class flash_sale_Home : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlashSaleItemId",
                table: "ShoppingCarts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FlashSalePrice",
                table: "ShoppingCarts",
                type: "decimal(18,2)",
                nullable: true);

            
            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
                table: "ShoppingCarts",
                column: "FlashSaleItemId",
                principalTable: "FlashSaleItems",
                principalColumn: "Id" );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_FlashSaleItemId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "FlashSaleItemId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "FlashSalePrice",
                table: "ShoppingCarts");
        }
    }
}
