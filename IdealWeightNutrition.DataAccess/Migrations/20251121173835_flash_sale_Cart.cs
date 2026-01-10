using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class flash_sale_Cart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlashSaleItemId",
                table: "orderDetails",
                type: "int",
                nullable: true );

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_FlashSaleItemId",
                table: "orderDetails",
                column: "FlashSaleItemId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_orderDetails_FlashSaleItems_FlashSaleItemId",
            //    table: "orderDetails",
            //    column: "FlashSaleItemId",
            //    principalTable: "FlashSaleItems",
            //    principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orderDetails_FlashSaleItems_FlashSaleItemId",
                table: "orderDetails");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_FlashSaleItemId",
                table: "orderDetails");

            migrationBuilder.DropColumn(
                name: "FlashSaleItemId",
                table: "orderDetails");
        }
    }
}
