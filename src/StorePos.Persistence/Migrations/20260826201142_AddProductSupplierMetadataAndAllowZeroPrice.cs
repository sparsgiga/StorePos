using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSupplierMetadataAndAllowZeroPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                schema: "dbo",
                table: "Products",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                schema: "dbo",
                table: "Products",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CostPrice_NonNegative",
                schema: "dbo",
                table: "Products",
                sql: "[CostPrice] IS NULL OR [CostPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                schema: "dbo",
                table: "Products",
                sql: "[Price] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CostPrice_NonNegative",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                schema: "dbo",
                table: "Products");
        }
    }
}
