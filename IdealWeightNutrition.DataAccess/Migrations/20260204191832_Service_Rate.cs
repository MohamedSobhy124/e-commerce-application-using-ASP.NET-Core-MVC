using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Service_Rate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companys_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ServiceSubscriptionId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved",
                table: "Reviews",
                columns: new[] { "ServiceSubscriptionId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved_CreatedAt",
                table: "Reviews",
                columns: new[] { "ServiceSubscriptionId", "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceSubscriptionId_UserId",
                table: "Reviews",
                columns: new[] { "ServiceSubscriptionId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companys_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId",
                principalTable: "Companys",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ServiceSubscriptions_ServiceSubscriptionId",
                table: "Reviews",
                column: "ServiceSubscriptionId",
                principalTable: "ServiceSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companys_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ServiceSubscriptions_ServiceSubscriptionId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_UserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ServiceSubscriptionId",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companys_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId",
                principalTable: "Companys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
