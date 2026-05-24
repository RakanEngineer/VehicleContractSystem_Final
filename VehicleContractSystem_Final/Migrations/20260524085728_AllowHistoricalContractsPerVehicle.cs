using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AllowHistoricalContractsPerVehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_ChassisNumber",
                table: "Contracts");

            migrationBuilder.AlterColumn<string>(
                name: "ChassisNumber",
                table: "Vehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ChassisNumber",
                table: "Vehicles",
                column: "ChassisNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_ChassisNumber",
                table: "Vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "ChassisNumber",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ChassisNumber",
                table: "Contracts",
                column: "ChassisNumber",
                unique: true);
        }
    }
}
