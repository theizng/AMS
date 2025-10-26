using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class AddNewProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveOccupants",
                table: "Rooms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveOccupants",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 1,
                column: "ActiveOccupants",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 2,
                column: "ActiveOccupants",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 3,
                column: "ActiveOccupants",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 4,
                column: "ActiveOccupants",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 5,
                column: "ActiveOccupants",
                value: 0);
        }
    }
}
