using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBarcodeUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [dbo].[Products]
                SET [Barcode] = NULL
                WHERE [Barcode] IS NOT NULL
                  AND LTRIM(RTRIM([Barcode])) = N'';

                IF EXISTS
                (
                    SELECT [Barcode]
                    FROM [dbo].[Products]
                    WHERE [Barcode] IS NOT NULL
                    GROUP BY [Barcode]
                    HAVING COUNT(*) > 1
                )
                    THROW 51010, 'Product barcode migration stopped: duplicate non-empty barcodes must be reviewed.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                schema: "dbo",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                schema: "dbo",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                schema: "dbo",
                table: "Products",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                schema: "dbo",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                schema: "dbo",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                schema: "dbo",
                table: "Products",
                column: "Barcode");
        }
    }
}
