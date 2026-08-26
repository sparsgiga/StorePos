using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenFinancialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[SaleItems]
                    WHERE [Quantity] <= 0
                       OR [UnitPrice] <= 0
                       OR ROUND([Quantity], 5) <= 0
                       OR ROUND([UnitPrice], 5) <= 0
                )
                    THROW 51000, 'Financial migration stopped: a sale item has an invalid quantity or unit price.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[SaleItems]
                    WHERE ROUND(ROUND([Quantity], 5) * ROUND([UnitPrice], 5), 2) < 0.01
                       OR ROUND(ROUND([Quantity], 5) * ROUND([UnitPrice], 5), 2) > 9999999999999999.99
                )
                    THROW 51001, 'Financial migration stopped: a canonical sale item total is outside decimal(18,2).', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[SalePayments]
                    WHERE [Amount] <= 0 OR ROUND([Amount], 2) <= 0
                )
                    THROW 51002, 'Financial migration stopped: a payment cannot be safely normalized to cents.', 1;

                IF EXISTS
                (
                    SELECT [SaleId]
                    FROM [dbo].[SaleItems]
                    GROUP BY [SaleId]
                    HAVING ROUND(SUM(ROUND(ROUND([Quantity], 5) * ROUND([UnitPrice], 5), 2)), 2)
                        > 9999999999999999.99
                )
                    THROW 51003, 'Financial migration stopped: a sale total is outside decimal(18,2).', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [dbo].[Sales] AS [s]
                    OUTER APPLY
                    (
                        SELECT ROUND(COALESCE(SUM(ROUND(ROUND([i].[Quantity], 5) * ROUND([i].[UnitPrice], 5), 2)), 0), 2) AS [NewTotal]
                        FROM [dbo].[SaleItems] AS [i]
                        WHERE [i].[SaleId] = [s].[Id]
                    ) AS [items]
                    OUTER APPLY
                    (
                        SELECT ROUND(COALESCE(SUM(ROUND([p].[Amount], 2)), 0), 2) AS [NewPaid],
                               COALESCE(SUM([p].[Amount]), 0) AS [OldPaid]
                        FROM [dbo].[SalePayments] AS [p]
                        WHERE [p].[SaleId] = [s].[Id]
                    ) AS [payments]
                    WHERE [s].[Status] = 2
                      AND
                      (
                          [payments].[NewPaid] > [items].[NewTotal]
                          OR
                          (
                              [payments].[OldPaid] >= [s].[TotalAmount]
                              AND [payments].[NewPaid] < [items].[NewTotal]
                          )
                      )
                )
                    THROW 51004, 'Financial migration stopped: normalization would create invalid completed-sale debt or overpayment.', 1;

                UPDATE [dbo].[SaleItems]
                SET [Quantity] = ROUND([Quantity], 5),
                    [UnitPrice] = ROUND([UnitPrice], 5);

                UPDATE [dbo].[SaleItems]
                SET [LineTotal] = ROUND([Quantity] * [UnitPrice], 2);

                UPDATE [dbo].[SalePayments]
                SET [Amount] = ROUND([Amount], 2);

                UPDATE [s]
                SET [TotalAmount] = [totals].[TotalAmount]
                FROM [dbo].[Sales] AS [s]
                CROSS APPLY
                (
                    SELECT ROUND(COALESCE(SUM([i].[LineTotal]), 0), 2) AS [TotalAmount]
                    FROM [dbo].[SaleItems] AS [i]
                    WHERE [i].[SaleId] = [s].[Id]
                ) AS [totals];
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                schema: "dbo",
                table: "Sales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);

            migrationBuilder.AddColumn<long>(
                name: "FinancialRevision",
                schema: "dbo",
                table: "Sales",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                schema: "dbo",
                table: "SalePayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                schema: "dbo",
                table: "SalePayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "LineTotal",
                schema: "dbo",
                table: "SaleItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,5)",
                oldPrecision: 18,
                oldScale: 5);

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_OperationId",
                schema: "dbo",
                table: "SalePayments",
                column: "OperationId",
                unique: true,
                filter: "[OperationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalePayments_OperationId",
                schema: "dbo",
                table: "SalePayments");

            migrationBuilder.DropColumn(
                name: "FinancialRevision",
                schema: "dbo",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OperationId",
                schema: "dbo",
                table: "SalePayments");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                schema: "dbo",
                table: "Sales",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                schema: "dbo",
                table: "SalePayments",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LineTotal",
                schema: "dbo",
                table: "SaleItems",
                type: "decimal(18,5)",
                precision: 18,
                scale: 5,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
