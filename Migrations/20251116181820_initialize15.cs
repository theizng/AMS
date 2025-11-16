using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_BikeExtraFee_NonNegative",
                table: "Rooms");

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
                name: "RoomCharges",
                columns: table => new
                {
                    RoomChargeId = table.Column<string>(type: "TEXT", nullable: false),
                    CycleId = table.Column<string>(type: "TEXT", nullable: false),
                    RoomCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BaseRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    UtilityFeesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CustomFeesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ElectricAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    WaterAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
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
                    table.CheckConstraint("CK_RoomCharge_Amounts_NonNegative", "[UtilityFeesTotal] >= 0 AND [CustomFeesTotal] >= 0 AND [ElectricAmount] >= 0 AND [WaterAmount] >= 0 AND [AmountPaid] >= 0");
                    table.CheckConstraint("CK_RoomCharge_BaseRent_NonNegative", "[BaseRent] >= 0");
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
                    FeeTypeId = table.Column<string>(type: "TEXT", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeeInstances");

            migrationBuilder.DropTable(
                name: "FeeTypes");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "RoomCharges");

            migrationBuilder.DropTable(
                name: "PaymentCycles");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_BikeExtraFee_NonNegative",
                table: "Rooms",
                sql: "[BikeExtraFee] IS NULL OR [BikeExtraFee] >= 0");
        }
    }
}
