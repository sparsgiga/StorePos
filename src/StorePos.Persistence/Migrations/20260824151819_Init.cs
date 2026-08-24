using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "dbo",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CashierId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CustomerIdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Users_CashierId",
                        column: x => x.CashierId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "dbo",
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleItems_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "dbo",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalePayments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "dbo",
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                schema: "dbo",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                schema: "dbo",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                schema: "dbo",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                schema: "dbo",
                table: "Products",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductId",
                schema: "dbo",
                table: "SaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                schema: "dbo",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_UnitId",
                schema: "dbo",
                table: "SaleItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_SaleId",
                schema: "dbo",
                table: "SalePayments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashierId",
                schema: "dbo",
                table: "Sales",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_DateCreated",
                schema: "dbo",
                table: "Sales",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleNumber",
                schema: "dbo",
                table: "Sales",
                column: "SaleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                schema: "dbo",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                schema: "dbo",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalePayments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sales",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Units",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");
        }
    }
}
