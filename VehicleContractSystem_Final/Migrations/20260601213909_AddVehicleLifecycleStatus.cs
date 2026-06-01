using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLifecycleStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Status",
                table: "Vehicles",
                column: "Status");

            migrationBuilder.Sql(@"
                UPDATE v
                SET Status = 1
                FROM Vehicles v
                WHERE EXISTS (
                    SELECT 1
                    FROM Contracts c
                    WHERE c.VehicleId = v.Id AND c.Status = 0
                );

                UPDATE v
                SET Status = 2
                FROM Vehicles v
                WHERE Status = 0
                  AND EXISTS (
                    SELECT 1
                    FROM Contracts c
                    WHERE c.VehicleId = v.Id AND c.Status = 1
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Status",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Vehicles");
        }
    }
}
