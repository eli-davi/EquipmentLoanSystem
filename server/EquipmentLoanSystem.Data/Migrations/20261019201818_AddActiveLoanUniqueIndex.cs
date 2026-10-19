using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentLoanSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveLoanUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId_ActiveUnique",
                table: "Loans",
                column: "ItemCopyId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_ItemCopyId_ActiveUnique",
                table: "Loans");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ItemCopyId",
                table: "Loans",
                column: "ItemCopyId");
        }
    }
}
