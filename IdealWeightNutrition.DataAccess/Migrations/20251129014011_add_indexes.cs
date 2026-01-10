using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockNotifications_ProductId",
                table: "StockNotifications");

            migrationBuilder.DropIndex(
                name: "IX_PromoCodeUsages_PromoCodeId",
                table: "PromoCodeUsages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "orderHeaders",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "orderHeaders",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_ApplicationUserId",
                table: "Wishlists",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockNotifications_ProductId_IsNotified",
                table: "StockNotifications",
                columns: new[] { "ProductId", "IsNotified" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "ShoppingCarts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSubscriptions_IsActive",
                table: "ServiceSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePurchases_ApplicationUserId_ServiceSubscriptionId",
                table: "ServicePurchases",
                columns: new[] { "ApplicationUserId", "ServiceSubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_IsApproved",
                table: "Reviews",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId_IsApproved",
                table: "Reviews",
                columns: new[] { "ProductId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId_IsApproved_CreatedAt",
                table: "Reviews",
                columns: new[] { "ProductId", "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId_UserId",
                table: "PromoCodeUsages",
                columns: new[] { "PromoCodeId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code_IsActive",
                table: "PromoCodes",
                columns: new[] { "Code", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_IsDeleted_ProductId_StockQuantity",
                table: "ProductVariants",
                columns: new[] { "IsDeleted", "ProductId", "StockQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_CategryId_StockQuantity",
                table: "Products",
                columns: new[] { "IsDeleted", "CategryId", "StockQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_Id",
                table: "Products",
                columns: new[] { "IsDeleted", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_Price",
                table: "Products",
                columns: new[] { "IsDeleted", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_StockQuantity_MinimumStockAlert",
                table: "Products",
                columns: new[] { "IsDeleted", "StockQuantity", "MinimumStockAlert" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_StockQuantity_Price",
                table: "Products",
                columns: new[] { "IsDeleted", "StockQuantity", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Price",
                table: "Products",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_IsDeleted_DisplayOrder",
                table: "ProductOptionValues",
                columns: new[] { "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_DisplayOrder",
                table: "ProductImages",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_ImageInfo",
                table: "ProductImages",
                columns: new[] { "ProductId", "ImageInfo" });

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_ApplicationUserId_OrderDate",
                table: "orderHeaders",
                columns: new[] { "ApplicationUserId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_ApplicationUserId_OrderStatus",
                table: "orderHeaders",
                columns: new[] { "ApplicationUserId", "OrderStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_Email",
                table: "orderHeaders",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_Email_OrderStatus",
                table: "orderHeaders",
                columns: new[] { "Email", "OrderStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_OrderDate",
                table: "orderHeaders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_OrderStatus",
                table: "orderHeaders",
                column: "OrderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_ProductId_OrderHeaderId",
                table: "orderDetails",
                columns: new[] { "ProductId", "OrderHeaderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_Email",
                table: "NewsletterSubscriptions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscriptions_Email_IsActive",
                table: "NewsletterSubscriptions",
                columns: new[] { "Email", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleItems_IsDeleted_FlashSaleQuantity",
                table: "FlashSaleItems",
                columns: new[] { "IsDeleted", "FlashSaleQuantity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wishlists_ApplicationUserId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_StockNotifications_ProductId_IsNotified",
                table: "StockNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSubscriptions_IsActive",
                table: "ServiceSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ServicePurchases_ApplicationUserId_ServiceSubscriptionId",
                table: "ServicePurchases");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_IsApproved",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId_IsApproved",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId_IsApproved_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_PromoCodeUsages_PromoCodeId_UserId",
                table: "PromoCodeUsages");

            migrationBuilder.DropIndex(
                name: "IX_PromoCodes_Code_IsActive",
                table: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_IsDeleted_ProductId_StockQuantity",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_CategryId_StockQuantity",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_Id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_Price",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_StockQuantity_MinimumStockAlert",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_StockQuantity_Price",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Price",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_IsDeleted_DisplayOrder",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_DisplayOrder",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_ImageInfo",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_ApplicationUserId_OrderDate",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_ApplicationUserId_OrderStatus",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_Email",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_Email_OrderStatus",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_OrderDate",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_OrderStatus",
                table: "orderHeaders");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_ProductId_OrderHeaderId",
                table: "orderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscriptions_Email",
                table: "NewsletterSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscriptions_Email_IsActive",
                table: "NewsletterSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_FlashSaleItems_IsDeleted_FlashSaleQuantity",
                table: "FlashSaleItems");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockNotifications_ProductId",
                table: "StockNotifications",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
