using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualProductCodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualProductCodeSequence",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NextCode = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualProductCodeSequence", x => x.Id);
                    table.CheckConstraint("CK_ManualProductCodeSequence_NextCode", "[NextCode] >= 1000");
                    table.CheckConstraint("CK_ManualProductCodeSequence_Singleton", "[Id] = 1");
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[ManualProductCodeSequence] ([Id], [NextCode])
                SELECT
                    1,
                    COALESCE(MAX([NumericCode]) + 1, CONVERT(bigint, 1000))
                FROM
                (
                    SELECT TRY_CONVERT(bigint, [Code]) AS [NumericCode]
                    FROM [dbo].[Products]
                ) AS [NumericProductCodes]
                WHERE [NumericCode] BETWEEN 1000 AND 9999;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualProductCodeSequence",
                schema: "dbo");
        }
    }
}
