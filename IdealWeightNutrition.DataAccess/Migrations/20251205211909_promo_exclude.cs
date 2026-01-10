using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class promo_exclude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromoCodeExcludedComboOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromoCodeId = table.Column<int>(type: "int", nullable: false),
                    ComboOfferId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeExcludedComboOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoCodeExcludedComboOffers_ComboOffers_ComboOfferId",
                        column: x => x.ComboOfferId,
                        principalTable: "ComboOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeExcludedComboOffers_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeExcludedComboOffers_ComboOfferId",
                table: "PromoCodeExcludedComboOffers",
                column: "ComboOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeExcludedComboOffers_PromoCodeId",
                table: "PromoCodeExcludedComboOffers",
                column: "PromoCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoCodeExcludedComboOffers");
        }
    }
}
