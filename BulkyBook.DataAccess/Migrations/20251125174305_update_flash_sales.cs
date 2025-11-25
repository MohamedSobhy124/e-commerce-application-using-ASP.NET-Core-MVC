using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class update_flash_sales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "FlashSaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashSaleItems_ProductVariants_ProductVariantId",
                table: "FlashSaleItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashSaleItems_ProductVariants_ProductVariantId",
                table: "FlashSaleItems");

            migrationBuilder.DropIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "FlashSaleItems");
        }
    }
}
