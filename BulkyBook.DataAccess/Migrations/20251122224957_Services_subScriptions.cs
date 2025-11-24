using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Services_subScriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    OfflinePaymentPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    PromoCodeId = table.Column<int>(type: "int", nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOffers_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceOffers_ServiceSubscriptions_ServiceSubscriptionId",
                        column: x => x.ServiceSubscriptionId,
                        principalTable: "ServiceSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GuestEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GuestPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentIntentId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceOfferId = table.Column<int>(type: "int", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePurchases_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServicePurchases_ServiceOffers_ServiceOfferId",
                        column: x => x.ServiceOfferId,
                        principalTable: "ServiceOffers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServicePurchases_ServiceSubscriptions_ServiceSubscriptionId",
                        column: x => x.ServiceSubscriptionId,
                        principalTable: "ServiceSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOffers_PromoCodeId",
                table: "ServiceOffers",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOffers_ServiceSubscriptionId",
                table: "ServiceOffers",
                column: "ServiceSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePurchases_ApplicationUserId",
                table: "ServicePurchases",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePurchases_ServiceOfferId",
                table: "ServicePurchases",
                column: "ServiceOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePurchases_ServiceSubscriptionId",
                table: "ServicePurchases",
                column: "ServiceSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePurchases");

            migrationBuilder.DropTable(
                name: "ServiceOffers");

            migrationBuilder.DropTable(
                name: "ServiceSubscriptions");
        }
    }
}
