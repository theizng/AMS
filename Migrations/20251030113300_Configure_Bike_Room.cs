using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class Configure_Bike_Room : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Houses_HouseID",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_HouseID",
                table: "Rooms");

            migrationBuilder.AlterColumn<int>(
                name: "RoomStatus",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Rooms",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Rooms",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "EmergencyContactRoomOccupancyId",
                table: "Rooms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    Plate = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OwnerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bikes_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "IdRoom",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 1,
                column: "EmergencyContactRoomOccupancyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 2,
                column: "EmergencyContactRoomOccupancyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 3,
                column: "EmergencyContactRoomOccupancyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 4,
                column: "EmergencyContactRoomOccupancyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "IdRoom",
                keyValue: 5,
                column: "EmergencyContactRoomOccupancyId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_EmergencyContactRoomOccupancyId",
                table: "Rooms",
                column: "EmergencyContactRoomOccupancyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseID_RoomCode",
                table: "Rooms",
                columns: new[] { "HouseID", "RoomCode" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_Area_Positive",
                table: "Rooms",
                sql: "[Area] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_BikeExtraFee_NonNegative",
                table: "Rooms",
                sql: "[BikeExtraFee] IS NULL OR [BikeExtraFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_FreeBikeAllowance_NonNegative",
                table: "Rooms",
                sql: "[FreeBikeAllowance] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_MaxOccupants_Positive",
                table: "Rooms",
                sql: "[MaxOccupants] >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_Price_NonNegative",
                table: "Rooms",
                sql: "[Price] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_Plate",
                table: "Bikes",
                column: "Plate");

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_RoomId_Plate",
                table: "Bikes",
                columns: new[] { "RoomId", "Plate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Houses_HouseID",
                table: "Rooms",
                column: "HouseID",
                principalTable: "Houses",
                principalColumn: "IdHouse",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomOccupancies_EmergencyContactRoomOccupancyId",
                table: "Rooms",
                column: "EmergencyContactRoomOccupancyId",
                principalTable: "RoomOccupancies",
                principalColumn: "IdRoomOccupancy",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Houses_HouseID",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomOccupancies_EmergencyContactRoomOccupancyId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "Bikes");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_EmergencyContactRoomOccupancyId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_HouseID_RoomCode",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_Area_Positive",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_BikeExtraFee_NonNegative",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_FreeBikeAllowance_NonNegative",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_MaxOccupants_Positive",
                table: "Rooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_Price_NonNegative",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRoomOccupancyId",
                table: "Rooms");

            migrationBuilder.AlterColumn<int>(
                name: "RoomStatus",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Rooms",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Rooms",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseID",
                table: "Rooms",
                column: "HouseID");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Houses_HouseID",
                table: "Rooms",
                column: "HouseID",
                principalTable: "Houses",
                principalColumn: "IdHouse",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
