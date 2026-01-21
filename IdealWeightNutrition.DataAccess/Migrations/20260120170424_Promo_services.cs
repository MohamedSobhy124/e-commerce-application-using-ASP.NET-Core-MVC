using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Promo_services : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeAllServices",
                table: "PromoCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PromoCodeExcludedServiceSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromoCodeId = table.Column<int>(type: "int", nullable: false),
                    ServiceSubscriptionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeExcludedServiceSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoCodeExcludedServiceSubscriptions_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeExcludedServiceSubscriptions_ServiceSubscriptions_ServiceSubscriptionId",
                        column: x => x.ServiceSubscriptionId,
                        principalTable: "ServiceSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeExcludedServiceSubscriptions_PromoCodeId",
                table: "PromoCodeExcludedServiceSubscriptions",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeExcludedServiceSubscriptions_ServiceSubscriptionId",
                table: "PromoCodeExcludedServiceSubscriptions",
                column: "ServiceSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoCodeExcludedServiceSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExcludeAllServices",
                table: "PromoCodes");
        }
    }
}
