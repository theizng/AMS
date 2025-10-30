using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initiliaze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "Houses",
                columns: table => new
                {
                    IdHouse = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    TotalRooms = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Houses", x => x.IdHouse);
                });

            migrationBuilder.CreateTable(
                name: "Bikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    Plate = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bikes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomOccupancies",
                columns: table => new
                {
                    IdRoomOccupancy = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoveInDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MoveOutDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DepositContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BikeCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomOccupancies", x => x.IdRoomOccupancy);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    IdRoom = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseID = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RoomStatus = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Area = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    MaxOccupants = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    FreeBikeAllowance = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    BikeExtraFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 100000m),
                    EmergencyContactRoomOccupancyId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    HouseIdHouse = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.IdRoom);
                    table.CheckConstraint("CK_Room_Area_Positive", "[Area] > 0");
                    table.CheckConstraint("CK_Room_BikeExtraFee_NonNegative", "[BikeExtraFee] IS NULL OR [BikeExtraFee] >= 0");
                    table.CheckConstraint("CK_Room_FreeBikeAllowance_NonNegative", "[FreeBikeAllowance] >= 0");
                    table.CheckConstraint("CK_Room_MaxOccupants_Positive", "[MaxOccupants] >= 1");
                    table.CheckConstraint("CK_Room_Price_NonNegative", "[Price] >= 0");
                    table.ForeignKey(
                        name: "FK_Rooms_Houses_HouseID",
                        column: x => x.HouseID,
                        principalTable: "Houses",
                        principalColumn: "IdHouse",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rooms_Houses_HouseIdHouse",
                        column: x => x.HouseIdHouse,
                        principalTable: "Houses",
                        principalColumn: "IdHouse");
                    table.ForeignKey(
                        name: "FK_Rooms_RoomOccupancies_EmergencyContactRoomOccupancyId",
                        column: x => x.EmergencyContactRoomOccupancyId,
                        principalTable: "RoomOccupancies",
                        principalColumn: "IdRoomOccupancy",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    IdTenant = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    IdCardNumber = table.Column<string>(type: "TEXT", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PermanentAddress = table.Column<string>(type: "TEXT", nullable: false),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: true),
                    MoveInDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MoveOutDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ContractUrl = table.Column<string>(type: "TEXT", nullable: true),
                    EmergencyContactsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.IdTenant);
                    table.ForeignKey(
                        name: "FK_Tenants_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "IdRoom");
                });

            migrationBuilder.InsertData(
                table: "Admin",
                columns: new[] { "AdminId", "CreatedAt", "Email", "FullName", "LastLogin", "PasswordHash", "PhoneNumber", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@example.com", "Quản Trị Viên", new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), "$2b$12$Dvin/fmQwvI7yF8PVrC//uRlbRmkTCzsFG1xO7xGOcG/N2QBITIqS", "0123456789", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_OwnerId",
                table: "Bikes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_Plate",
                table: "Bikes",
                column: "Plate");

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_RoomId_Plate",
                table: "Bikes",
                columns: new[] { "RoomId", "Plate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupancies_RoomId_MoveOutDate",
                table: "RoomOccupancies",
                columns: new[] { "RoomId", "MoveOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupancies_TenantId_MoveOutDate",
                table: "RoomOccupancies",
                columns: new[] { "TenantId", "MoveOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_EmergencyContactRoomOccupancyId",
                table: "Rooms",
                column: "EmergencyContactRoomOccupancyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseID_RoomCode",
                table: "Rooms",
                columns: new[] { "HouseID", "RoomCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseIdHouse",
                table: "Rooms",
                column: "HouseIdHouse");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_RoomId",
                table: "Tenants",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bikes_Rooms_RoomId",
                table: "Bikes",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "IdRoom",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bikes_Tenants_OwnerId",
                table: "Bikes",
                column: "OwnerId",
                principalTable: "Tenants",
                principalColumn: "IdTenant",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomOccupancies_Rooms_RoomId",
                table: "RoomOccupancies",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "IdRoom",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomOccupancies_Tenants_TenantId",
                table: "RoomOccupancies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "IdTenant",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomOccupancies_Rooms_RoomId",
                table: "RoomOccupancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Rooms_RoomId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "Bikes");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Houses");

            migrationBuilder.DropTable(
                name: "RoomOccupancies");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
