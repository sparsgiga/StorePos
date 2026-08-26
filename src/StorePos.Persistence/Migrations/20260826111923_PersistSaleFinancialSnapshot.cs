using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistSaleFinancialSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                schema: "dbo",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "dbo",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE [sale]
                SET
                    [PaidAmount] = CASE
                        WHEN [sale].[CompletionVersion] = 0 THEN CAST(0 AS decimal(18,2))
                        ELSE [current].[PaidAmount]
                    END,
                    [OutstandingAmount] = CASE
                        WHEN [sale].[CompletionVersion] = 0 THEN CAST(0 AS decimal(18,2))
                        WHEN [sale].[TotalAmount] > [current].[PaidAmount]
                            THEN CAST(ROUND([sale].[TotalAmount] - [current].[PaidAmount], 2) AS decimal(18,2))
                        ELSE CAST(0 AS decimal(18,2))
                    END
                FROM [dbo].[Sales] AS [sale]
                OUTER APPLY
                (
                    SELECT CAST(
                        ROUND(COALESCE(SUM([payment].[Amount]), 0), 2)
                        AS decimal(18,2)) AS [PaidAmount]
                    FROM [dbo].[SalePayments] AS [payment]
                    WHERE [payment].[SaleId] = [sale].[Id]
                      AND [payment].[CompletionVersion] = [sale].[CompletionVersion]
                      AND [payment].[PaymentKind] IN (1, 2)
                ) AS [current];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                schema: "dbo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "dbo",
                table: "Sales");
        }
    }
}
