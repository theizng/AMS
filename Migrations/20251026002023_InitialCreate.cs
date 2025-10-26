using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "Rooms",
                columns: table => new
                {
                    IdRoom = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HouseID = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomCode = table.Column<string>(type: "TEXT", nullable: false),
                    RoomStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Area = table.Column<decimal>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    MaxOccupants = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    FreeBikeAllowance = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    BikeExtraFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 100000m),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    HouseIdHouse = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.IdRoom);
                    table.ForeignKey(
                        name: "FK_Rooms_Houses_HouseID",
                        column: x => x.HouseID,
                        principalTable: "Houses",
                        principalColumn: "IdHouse",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rooms_Houses_HouseIdHouse",
                        column: x => x.HouseIdHouse,
                        principalTable: "Houses",
                        principalColumn: "IdHouse");
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
                    table.ForeignKey(
                        name: "FK_RoomOccupancies_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "IdRoom",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomOccupancies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "IdTenant",
                        onDelete: ReferentialAction.Restrict);
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
                columns: new[] { "IdRoom", "Area", "CreatedAt", "FreeBikeAllowance", "HouseID", "HouseIdHouse", "Notes", "Price", "RoomCode", "RoomStatus", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 25m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, "Phòng thường", 3000000m, "COCONUT", 0, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 25m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, "Phòng thường", 3000000m, "APPLE", 0, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 30m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, "Phòng thường, có 1 con mèo", 3500000m, "BANANA", 0, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 35m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, "Phòng có đồ cơ bản, có 2 con chó", 4000000m, "PAPAYA", 0, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 35m, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, "Phòng có đồ cơ bản", 4000000m, "STRAWBERRY", 0, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupancies_RoomId_MoveOutDate",
                table: "RoomOccupancies",
                columns: new[] { "RoomId", "MoveOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupancies_TenantId_MoveOutDate",
                table: "RoomOccupancies",
                columns: new[] { "TenantId", "MoveOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseID",
                table: "Rooms",
                column: "HouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HouseIdHouse",
                table: "Rooms",
                column: "HouseIdHouse");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_RoomId",
                table: "Tenants",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "RoomOccupancies");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Houses");
        }
    }
}
