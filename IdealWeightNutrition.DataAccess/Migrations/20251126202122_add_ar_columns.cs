using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_ar_columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wishlists_ApplicationUserId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantId",
                table: "ProductVariantOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_OrderHeaderId",
                table: "orderDetails");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ValueAr",
                table: "ProductOptionValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "ProductOptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_ApplicationUserId_ProductId",
                table: "Wishlists",
                columns: new[] { "ApplicationUserId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ApplicationUserId_ProductId_ProductVariantId",
                table: "ShoppingCarts",
                columns: new[] { "ApplicationUserId", "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId_UserId",
                table: "Reviews",
                columns: new[] { "ProductId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_IsDeleted_ProductId",
                table: "ProductVariants",
                columns: new[] { "IsDeleted", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_StockQuantity",
                table: "ProductVariants",
                column: "StockQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantId_ProductOptionValueId",
                table: "ProductVariantOptionValues",
                columns: new[] { "ProductVariantId", "ProductOptionValueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_CategryId",
                table: "Products",
                columns: new[] { "IsDeleted", "CategryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_StockQuantity",
                table: "Products",
                columns: new[] { "IsDeleted", "StockQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StockQuantity",
                table: "Products",
                column: "StockQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_IsDeleted_ProductOptionId",
                table: "ProductOptionValues",
                columns: new[] { "IsDeleted", "ProductOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_IsDeleted_ProductId",
                table: "ProductOptions",
                columns: new[] { "IsDeleted", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_OrderHeaderId_ProductId",
                table: "orderDetails",
                columns: new[] { "OrderHeaderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_IsActive_StartDate_EndDate",
                table: "FlashSales",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_IsDeleted_IsActive_StartDate_EndDate",
                table: "FlashSales",
                columns: new[] { "IsDeleted", "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_IsDeleted_FlashSaleId_ProductId",
                table: "FlashSaleItems",
                columns: new[] { "IsDeleted", "FlashSaleId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Categries_IsDeleted",
                table: "Categries",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wishlists_ApplicationUserId_ProductId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ApplicationUserId_ProductId_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_IsDeleted_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_StockQuantity",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantId_ProductOptionValueId",
                table: "ProductVariantOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_CategryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_StockQuantity",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StockQuantity",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_IsDeleted_ProductOptionId",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_IsDeleted_ProductId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_OrderHeaderId_ProductId",
                table: "orderDetails");

            migrationBuilder.DropIndex(
                name: "IX_FlashSales_IsActive_StartDate_EndDate",
                table: "FlashSales");

            migrationBuilder.DropIndex(
                name: "IX_FlashSales_IsDeleted_IsActive_StartDate_EndDate",
                table: "FlashSales");

            migrationBuilder.DropIndex(
                name: "IX_FlashSaleItems_IsDeleted_FlashSaleId_ProductId",
                table: "FlashSaleItems");

            migrationBuilder.DropIndex(
                name: "IX_Categries_IsDeleted",
                table: "Categries");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ValueAr",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "ProductOptions");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_ApplicationUserId",
                table: "Wishlists",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "ShoppingCarts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantId",
                table: "ProductVariantOptionValues",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_OrderHeaderId",
                table: "orderDetails",
                column: "OrderHeaderId");
        }
    }
}
