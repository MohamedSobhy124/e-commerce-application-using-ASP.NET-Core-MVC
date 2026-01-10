using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class orderAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SlugAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SlugAr",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "SlugEn",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "OrderAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderHeaderId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PerformedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OldOrderStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewOrderStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldPaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewPaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAuditLogs_orderHeaders_OrderHeaderId",
                        column: x => x.OrderHeaderId,
                        principalTable: "orderHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAuditLogs_OrderHeaderId",
                table: "OrderAuditLogs",
                column: "OrderHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAuditLogs_OrderHeaderId_ActionDate",
                table: "OrderAuditLogs",
                columns: new[] { "OrderHeaderId", "ActionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAuditLogs_PerformedByUserId",
                table: "OrderAuditLogs",
                column: "PerformedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "SlugEn",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugAr",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SlugAr",
                table: "Products",
                column: "SlugAr",
                unique: true,
                filter: "[SlugAr] IS NOT NULL");
        }
    }
}
