using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleContractSystem_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddContractVerificationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "Contracts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Contracts
                SET VerificationToken = REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')
                WHERE VerificationToken IS NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_VerificationToken",
                table: "Contracts",
                column: "VerificationToken",
                unique: true,
                filter: "[VerificationToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_VerificationToken",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "Contracts");
        }
    }
}
