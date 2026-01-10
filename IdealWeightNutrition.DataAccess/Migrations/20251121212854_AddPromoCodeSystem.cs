using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCodeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
            //    table: "ShoppingCarts");

            migrationBuilder.AlterColumn<int>(
                name: "FlashSaleItemId",
                table: "ShoppingCarts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "DiscountAmount",
                table: "orderHeaders",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OrderSubtotal",
                table: "orderHeaders",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromoCodeId",
                table: "orderHeaders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeText",
                table: "orderHeaders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaximumDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    UsageLimitPerUser = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodeUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromoCodeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_orderHeaders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orderHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orderHeaders_PromoCodeId",
                table: "orderHeaders",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_OrderId",
                table: "PromoCodeUsages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_UserId",
                table: "PromoCodeUsages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_orderHeaders_PromoCodes_PromoCodeId",
                table: "orderHeaders",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
            //    table: "ShoppingCarts",
            //    column: "FlashSaleItemId",
            //    principalTable: "FlashSaleItems",
            //    principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orderHeaders_PromoCodes_PromoCodeId",
                table: "orderHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
                table: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "PromoCodeUsages");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_orderHeaders_PromoCodeId",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "OrderSubtotal",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "orderHeaders");

            migrationBuilder.DropColumn(
                name: "PromoCodeText",
                table: "orderHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "FlashSaleItemId",
                table: "ShoppingCarts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId",
                table: "ShoppingCarts",
                column: "FlashSaleItemId",
                principalTable: "FlashSaleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
