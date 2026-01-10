using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealWeightNutrition.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_expirydate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Products",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Products");
        }
    }
}
