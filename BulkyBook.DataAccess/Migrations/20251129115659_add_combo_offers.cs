using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_combo_offers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComboOfferId",
                table: "ShoppingCarts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComboOfferId",
                table: "orderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComboOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ComboPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinimumQuantity = table.Column<int>(type: "int", nullable: false),
                    MaximumQuantity = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComboOfferItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComboOfferId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboOfferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComboOfferItems_ComboOffers_ComboOfferId",
                        column: x => x.ComboOfferId,
                        principalTable: "ComboOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboOfferItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComboOfferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ComboOfferId",
                table: "ShoppingCarts",
                column: "ComboOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_orderDetails_ComboOfferId",
                table: "orderDetails",
                column: "ComboOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboOfferItems_ComboOfferId",
                table: "ComboOfferItems",
                column: "ComboOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboOfferItems_IsDeleted_ComboOfferId_ProductId",
                table: "ComboOfferItems",
                columns: new[] { "IsDeleted", "ComboOfferId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboOfferItems_ProductId",
                table: "ComboOfferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboOfferItems_ProductVariantId",
                table: "ComboOfferItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboOffers_IsDeleted_IsActive_StartDate_EndDate",
                table: "ComboOffers",
                columns: new[] { "IsDeleted", "IsActive", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_orderDetails_ComboOffers_ComboOfferId",
                table: "orderDetails",
                column: "ComboOfferId",
                principalTable: "ComboOffers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_ComboOffers_ComboOfferId",
                table: "ShoppingCarts",
                column: "ComboOfferId",
                principalTable: "ComboOffers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orderDetails_ComboOffers_ComboOfferId",
                table: "orderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_ComboOffers_ComboOfferId",
                table: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "ComboOfferItems");

            migrationBuilder.DropTable(
                name: "ComboOffers");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ComboOfferId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_orderDetails_ComboOfferId",
                table: "orderDetails");

            migrationBuilder.DropColumn(
                name: "ComboOfferId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "ComboOfferId",
                table: "orderDetails");
        }
    }
}
