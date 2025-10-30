using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initiliaze_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Houses",
                keyColumn: "IdHouse",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Houses",
                columns: new[] { "IdHouse", "Address", "CreatedAt", "Notes", "TotalRooms", "UpdatedAt" },
                values: new object[] { 1, "123 Đường ABC, Quận XYZ, TP HCM", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), "Nhà cho thuê 10 phòng", 10, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "IdRoom", "Area", "CreatedAt", "EmergencyContactRoomOccupancyId", "FreeBikeAllowance", "HouseID", "HouseIdHouse", "Notes", "Price", "RoomCode", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 25m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null, "Phòng thường", 3000000m, "COCONUT", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 25m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null, "Phòng thường", 3000000m, "APPLE", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 30m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null, "Phòng thường, có 1 con mèo", 3500000m, "BANANA", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 35m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null, "Phòng có đồ cơ bản, có 2 con chó", 4000000m, "PAPAYA", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 35m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null, "Phòng có đồ cơ bản", 4000000m, "STRAWBERRY", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}
