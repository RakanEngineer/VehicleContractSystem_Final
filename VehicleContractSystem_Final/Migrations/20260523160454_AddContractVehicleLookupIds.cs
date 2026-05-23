using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddContractVehicleLookupIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColorId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleModelId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleTypeId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ColorId",
                table: "Contracts",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_VehicleModelId",
                table: "Contracts",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_VehicleTypeId",
                table: "Contracts",
                column: "VehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Colors_ColorId",
                table: "Contracts",
                column: "ColorId",
                principalTable: "Colors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_VehicleModels_VehicleModelId",
                table: "Contracts",
                column: "VehicleModelId",
                principalTable: "VehicleModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_VehicleTypes_VehicleTypeId",
                table: "Contracts",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Colors_ColorId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_VehicleModels_VehicleModelId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_VehicleTypes_VehicleTypeId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ColorId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_VehicleModelId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_VehicleTypeId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ColorId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleModelId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "Contracts");
        }
    }
}
