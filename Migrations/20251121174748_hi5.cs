using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class hi5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomCharge_Amounts_NonNegative",
                table: "RoomCharges");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomCharge_NonNegative",
                table: "RoomCharges",
                sql: "[CustomFeesTotal] >= 0 AND [ElectricAmount] >= 0 AND [WaterAmount] >= 0 AND [AmountPaid] >= 0 AND [BaseRent] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomCharge_NonNegative",
                table: "RoomCharges");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomCharge_Amounts_NonNegative",
                table: "RoomCharges",
                sql: "[UtilityFeesTotal] >= 0 AND [CustomFeesTotal] >= 0 AND [ElectricAmount] >= 0 AND [WaterAmount] >= 0 AND [AmountPaid] >= 0");
        }
    }
}
