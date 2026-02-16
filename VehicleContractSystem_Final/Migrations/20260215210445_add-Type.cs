using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class addType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Color_ColorId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleModel_VehicleModelId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleModel",
                table: "VehicleModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Color",
                table: "Color");

            migrationBuilder.RenameTable(
                name: "VehicleModel",
                newName: "VehicleModels");

            migrationBuilder.RenameTable(
                name: "Color",
                newName: "Colors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleModels",
                table: "VehicleModels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Colors",
                table: "Colors",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Colors_ColorId",
                table: "Vehicles",
                column: "ColorId",
                principalTable: "Colors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleModels_VehicleModelId",
                table: "Vehicles",
                column: "VehicleModelId",
                principalTable: "VehicleModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Colors_ColorId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleModels_VehicleModelId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleModels",
                table: "VehicleModels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Colors",
                table: "Colors");

            migrationBuilder.RenameTable(
                name: "VehicleModels",
                newName: "VehicleModel");

            migrationBuilder.RenameTable(
                name: "Colors",
                newName: "Color");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleModel",
                table: "VehicleModel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Color",
                table: "Color",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Color_ColorId",
                table: "Vehicles",
                column: "ColorId",
                principalTable: "Color",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleModel_VehicleModelId",
                table: "Vehicles",
                column: "VehicleModelId",
                principalTable: "VehicleModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
