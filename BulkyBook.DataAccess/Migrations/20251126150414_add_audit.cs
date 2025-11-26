using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class add_audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductVariants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ProductVariants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductVariants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Products",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Products",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductOptionValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ProductOptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductOptionValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductOptionValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ProductOptionValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductOptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "ProductOptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductOptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ProductOptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FlashSales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FlashSales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "FlashSales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "FlashSales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FlashSaleItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "FlashSaleItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FlashSaleItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "FlashSaleItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "FlashSaleItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Categries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Categries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Categries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Categries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Categries",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "FlashSaleItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Categries");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Categries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Categries");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Categries");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Categries");
        }
    }
}
