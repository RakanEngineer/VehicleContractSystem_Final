using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChassisNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockedVehicles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedVehicles_ChassisNumber",
                table: "BlockedVehicles",
                column: "ChassisNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedVehicles_CreatedByUserId",
                table: "BlockedVehicles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedVehicles_IsActive",
                table: "BlockedVehicles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedVehicles_LicenseNumber",
                table: "BlockedVehicles",
                column: "LicenseNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedVehicles");
        }
    }
}
