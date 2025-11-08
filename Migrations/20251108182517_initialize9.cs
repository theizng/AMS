using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RoomCode",
                table: "Contracts",
                column: "RoomCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contracts");
        }
    }
}
