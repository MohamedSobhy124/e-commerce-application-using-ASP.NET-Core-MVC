using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAdditionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthNotes",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthNotesAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedUse",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedUseAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthNotes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HealthNotesAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Specification",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecificationAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SuggestedUse",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SuggestedUseAr",
                table: "Products");
        }
    }
}
