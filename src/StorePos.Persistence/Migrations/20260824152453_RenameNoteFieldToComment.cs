using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameNoteFieldToComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Note",
                schema: "dbo",
                table: "SaleItems",
                newName: "Comment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                schema: "dbo",
                table: "SaleItems",
                newName: "Note");
        }
    }
}
