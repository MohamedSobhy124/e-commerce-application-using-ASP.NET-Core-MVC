using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceReviewsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make ProductId nullable
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Add ServiceSubscriptionId column
            migrationBuilder.AddColumn<int>(
                name: "ServiceSubscriptionId",
                table: "Reviews",
                type: "int",
                nullable: true);

            // Create index for service reviews
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

            // Add foreign key for ServiceSubscription
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
            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ServiceSubscriptions_ServiceSubscriptionId",
                table: "Reviews");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_IsApproved_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ServiceSubscriptionId_UserId",
                table: "Reviews");

            // Remove ServiceSubscriptionId column
            migrationBuilder.DropColumn(
                name: "ServiceSubscriptionId",
                table: "Reviews");

            // Make ProductId required again
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
