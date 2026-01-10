using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class product_info_image : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageInfo",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageInfo",
                table: "ProductImages");
        }
    }
}
