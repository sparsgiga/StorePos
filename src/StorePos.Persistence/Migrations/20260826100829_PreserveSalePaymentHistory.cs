using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveSalePaymentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionVersion",
                schema: "dbo",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompletionVersion",
                schema: "dbo",
                table: "SalePayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[SalePayments] AS [p]
                    LEFT JOIN [dbo].[Sales] AS [s] ON [s].[Id] = [p].[SaleId]
                    WHERE [s].[Id] IS NULL OR [s].[Status] <> 2
                )
                    THROW 51020, 'Payment history migration stopped: a payment belongs to a sale that is not completed.', 1;

                UPDATE [dbo].[Sales]
                SET [CompletionVersion] = CASE WHEN [Status] = 2 THEN 1 ELSE 0 END;

                UPDATE [p]
                SET [CompletionVersion] = 1
                FROM [dbo].[SalePayments] AS [p]
                INNER JOIN [dbo].[Sales] AS [s] ON [s].[Id] = [p].[SaleId]
                WHERE [s].[Status] = 2;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_SaleId_CompletionVersion",
                schema: "dbo",
                table: "SalePayments",
                columns: new[] { "SaleId", "CompletionVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalePayments_SaleId_CompletionVersion",
                schema: "dbo",
                table: "SalePayments");

            migrationBuilder.DropColumn(
                name: "CompletionVersion",
                schema: "dbo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CompletionVersion",
                schema: "dbo",
                table: "SalePayments");
        }
    }
}
