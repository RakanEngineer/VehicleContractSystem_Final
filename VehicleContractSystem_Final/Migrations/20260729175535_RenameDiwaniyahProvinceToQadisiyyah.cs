using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiwaniyahProvinceToQadisiyyah : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "القادسية", "Al-Qadisiyyah" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Provinces",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "NameAr", "NameEn" },
                values: new object[] { "الديوانية", "Diwaniyah" });
        }
    }
}
