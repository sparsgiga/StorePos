using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StorePos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMeasurementUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Units",
                columns: new[] { "Id", "Code", "DateCreated", "DateUpdated", "IsActive", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ცალი", "ც" },
                    { 2, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კილოგრამი", "კგ" },
                    { 3, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "მეტრი", "მ" },
                    { 4, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ლიტრი", "ლ" },
                    { 5, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "გრამი", "გრ" },
                    { 6, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ტონა", "ტ" },
                    { 7, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "წყვილი (2 ცალი)", "წყ" },
                    { 8, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "სანტიმეტრი", "სმ" },
                    { 9, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კვადრატული მეტრი", "კვ.მ" },
                    { 10, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ყუთი", "ყთ" },
                    { 11, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ქილა", "ქ" },
                    { 12, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ბოთლი", "ბთ" },
                    { 13, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კილომეტრი", "კმ" },
                    { 14, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კვადრატული სანტიმეტრი", "კვ.სმ" },
                    { 15, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კუბური მეტრი", "კბ.მ" },
                    { 16, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "მილილიტრი", "მმ" },
                    { 17, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "სხვა", "სხვ" },
                    { 18, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ჭიქა", "ჭიქა" },
                    { 19, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "კომპლექტი", "კომპლექტი" },
                    { 20, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "რულონი", "რულონი" },
                    { 21, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "ტომარა", "ტომარა" },
                    { 22, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "შეკვრა", "შეკვრა" },
                    { 24, null, new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "მ³", "მ³" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Units",
                keyColumn: "Id",
                keyValue: 24);
        }
    }
}
