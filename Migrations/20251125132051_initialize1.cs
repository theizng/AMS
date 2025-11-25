using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize1 : Migration
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
                    IdCardNumber = table.Column<string>(type: "TEXT", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RoomCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HouseAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    TenantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DueDay = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentMethods = table.Column<string>(type: "TEXT", nullable: false),
                    LateFeePolicy = table.Column<string>(type: "TEXT", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DepositReturnDays = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxOccupants = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxBikeAllowance = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyDescription = table.Column<string>(type: "TEXT", nullable: false),
                    PdfUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    NeedsAddendum = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AddendumNotifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                });

            migrationBuilder.CreateTable(
                name: "FeeTypes",
                columns: table => new
                {
                    FeeTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UnitLabel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DefaultRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ApplyAllRooms = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeTypes", x => x.FeeTypeId);
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
                name: "PaymentCycles",
                columns: table => new
                {
                    CycleId = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Closed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCycles", x => x.CycleId);
                });

            migrationBuilder.CreateTable(
                name: "ContractAddendums",
                columns: table => new
                {
                    AddendumId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentContractId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AddendumNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    OldTenantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    NewTenantsJson = table.Column<string>(type: "TEXT", nullable: false),
                    OldSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    NewSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PdfUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAddendums", x => x.AddendumId);
                    table.ForeignKey(
                        name: "FK_ContractAddendums_Contracts_ParentContractId",
                        column: x => x.ParentContractId,
                        principalTable: "Contracts",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomCharges",
                columns: table => new
                {
                    RoomChargeId = table.Column<string>(type: "TEXT", nullable: false),
                    CycleId = table.Column<string>(type: "TEXT", nullable: false),
                    RoomCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BaseRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CustomFeesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ElectricAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    WaterAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    BikePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    FirstSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastReminderSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ElectricPrev = table.Column<int>(type: "INTEGER", nullable: true),
                    ElectricCur = table.Column<int>(type: "INTEGER", nullable: true),
                    ElectricRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    ElectricConfirmed = table.Column<bool>(type: "INTEGER", nullable: true, defaultValue: false),
                    WaterPrev = table.Column<int>(type: "INTEGER", nullable: true),
                    WaterCur = table.Column<int>(type: "INTEGER", nullable: true),
                    WaterRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    WaterConfirmed = table.Column<bool>(type: "INTEGER", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomCharges", x => x.RoomChargeId);
                    table.CheckConstraint("CK_RoomCharge_BaseRent_NonNegative", "[BaseRent] >= 0");
                    table.CheckConstraint("CK_RoomCharge_NonNegative", "[CustomFeesTotal] >= 0 AND [ElectricAmount] >= 0 AND [WaterAmount] >= 0 AND [AmountPaid] >= 0 AND [BaseRent] >= 0");
                    table.ForeignKey(
                        name: "FK_RoomCharges_PaymentCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PaymentCycles",
                        principalColumn: "CycleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeeInstances",
                columns: table => new
                {
                    FeeInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    RoomChargeId = table.Column<string>(type: "TEXT", nullable: false),
                    FeeTypeId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 1m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeInstances", x => x.FeeInstanceId);
                    table.CheckConstraint("CK_FeeInstance_NonNegative", "[Rate] >= 0 AND [Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_FeeInstances_RoomCharges_RoomChargeId",
                        column: x => x.RoomChargeId,
                        principalTable: "RoomCharges",
                        principalColumn: "RoomChargeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    PaymentRecordId = table.Column<string>(type: "TEXT", nullable: false),
                    RoomChargeId = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    IsPartial = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.PaymentRecordId);
                    table.CheckConstraint("CK_PaymentRecord_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentRecords_RoomCharges_RoomChargeId",
                        column: x => x.RoomChargeId,
                        principalTable: "RoomCharges",
                        principalColumn: "RoomChargeId",
                        onDelete: ReferentialAction.Cascade);
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
                    MaxBikeAllowance = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
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
                    table.CheckConstraint("CK_Room_FreeBikeAllowance_NonNegative", "[FreeBikeAllowance] >= 0");
                    table.CheckConstraint("CK_Room_FreeBikeAllowance_Within_Max", "([MaxBikeAllowance] = 0 OR [FreeBikeAllowance] <= [MaxBikeAllowance])");
                    table.CheckConstraint("CK_Room_MaxBikeAllowance_NonNegative", "[MaxBikeAllowance] >= 0");
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
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
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
                columns: new[] { "AdminId", "CreatedAt", "Email", "FullName", "IdCardNumber", "LastLogin", "PasswordHash", "PhoneNumber", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@example.com", "Quản Trị Viên", null, new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc), "$2b$12$Dvin/fmQwvI7yF8PVrC//uRlbRmkTCzsFG1xO7xGOcG/N2QBITIqS", "0123456789", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin" });

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
                name: "IX_ContractAddendums_AddendumNumber",
                table: "ContractAddendums",
                column: "AddendumNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddendums_ParentContractId",
                table: "ContractAddendums",
                column: "ParentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RoomCode",
                table: "Contracts",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInstances_RoomChargeId",
                table: "FeeInstances",
                column: "RoomChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeTypes_Name",
                table: "FeeTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCycles_Year_Month",
                table: "PaymentCycles",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_RoomChargeId",
                table: "PaymentRecords",
                column: "RoomChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomCharges_CycleId_RoomCode",
                table: "RoomCharges",
                columns: new[] { "CycleId", "RoomCode" },
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
                name: "IX_Rooms_RoomCode",
                table: "Rooms",
                column: "RoomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_FullName",
                table: "Tenants",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PhoneNumber",
                table: "Tenants",
                column: "PhoneNumber");

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
                onDelete: ReferentialAction.Cascade);

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
                name: "ContractAddendums");

            migrationBuilder.DropTable(
                name: "FeeInstances");

            migrationBuilder.DropTable(
                name: "FeeTypes");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "RoomCharges");

            migrationBuilder.DropTable(
                name: "PaymentCycles");

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
