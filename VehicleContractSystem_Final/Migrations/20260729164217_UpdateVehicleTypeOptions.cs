using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleTypeOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "خصوصي", "Private" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "حمل", "Cargo" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "أجرة", "Taxi" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "زراعية", "Agricultural" });

            migrationBuilder.InsertData(
                table: "VehicleTypes",
                columns: new[] { "Id", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { 5, "إنشائية", "Construction" },
                    { 6, "فحص مؤقت", "Temporary Inspection" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "سيارة", "Car" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "دراجة نارية", "Motorcycle" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "شاحنة", "Truck" });

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "حافلة", "Bus" });
        }
    }
}
