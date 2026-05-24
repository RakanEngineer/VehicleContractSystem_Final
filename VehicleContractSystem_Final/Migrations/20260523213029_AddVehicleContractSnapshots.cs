using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleContractSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Vehicles",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ChassisNumberSnapshot",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumberSnapshot",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufactureYearSnapshot",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleColorNameSnapshot",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleModelNameSnapshot",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleTypeNameSnapshot",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_VehicleId",
                table: "Contracts",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Vehicles_VehicleId",
                table: "Contracts",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Vehicles_VehicleId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_VehicleId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ChassisNumberSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LicenseNumberSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ManufactureYearSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleColorNameSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleModelNameSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleTypeNameSnapshot",
                table: "Contracts");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Vehicles",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
