using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceBlockedVehicleCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlateCode",
                table: "BlockedVehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "BlockedVehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "ChassisNumber",
                table: "BlockedVehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "BlockedVehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "BlockedVehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "BlockedVehicles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockedVehicles_DeactivatedByUserId",
                table: "BlockedVehicles",
                column: "DeactivatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockedVehicles_AspNetUsers_DeactivatedByUserId",
                table: "BlockedVehicles",
                column: "DeactivatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockedVehicles_AspNetUsers_DeactivatedByUserId",
                table: "BlockedVehicles");

            migrationBuilder.DropIndex(
                name: "IX_BlockedVehicles_DeactivatedByUserId",
                table: "BlockedVehicles");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "BlockedVehicles");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "BlockedVehicles");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "BlockedVehicles");

            migrationBuilder.AlterColumn<string>(
                name: "PlateCode",
                table: "BlockedVehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "BlockedVehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChassisNumber",
                table: "BlockedVehicles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
