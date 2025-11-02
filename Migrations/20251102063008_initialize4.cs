using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxBikeAllowance",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_FreeBikeAllowance_Within_Max",
                table: "Rooms",
                sql: "([MaxBikeAllowance] = 0 OR [FreeBikeAllowance] <= [MaxBikeAllowance])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_MaxBikeAllowance_NonNegative",
                table: "Rooms",
                sql: "[MaxBikeAllowance] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_FreeBikeAllowance_Within_Max",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_MaxBikeAllowance_NonNegative",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MaxBikeAllowance",
                table: "Rooms");
        }
    }
}
