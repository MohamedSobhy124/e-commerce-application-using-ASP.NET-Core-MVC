using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesForSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Products",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_CategryId_Price_Composite",
                table: "Products",
                columns: new[] { "IsDeleted", "CategryId", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_IsNew",
                table: "Products",
                columns: new[] { "IsDeleted", "IsNew" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_IsNew_IsTrending",
                table: "Products",
                columns: new[] { "IsDeleted", "IsNew", "IsTrending" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_IsTrending",
                table: "Products",
                columns: new[] { "IsDeleted", "IsTrending" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Title",
                table: "Products",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TitleAr",
                table: "Products",
                column: "TitleAr");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems",
                column: "ProductVariantId",
                filter: "[ProductVariantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_CategryId_Price_Composite",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_IsNew",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_IsNew_IsTrending",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_IsTrending",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Title",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TitleAr",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_ProductVariantId",
                table: "FlashSaleItems",
                column: "ProductVariantId");
        }
    }
}
