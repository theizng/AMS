using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class hi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "FeeTypeId",
                table: "FeeInstances",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FeeTypeId",
                table: "FeeInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    BillingMonth = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Extras = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    UniqueCode = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Utilities = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoice_Paid_Le_Total", "[PaidAmount] <= [TotalAmount]");
                    table.CheckConstraint("CK_Invoice_Paid_NonNegative", "[PaidAmount] >= 0");
                    table.CheckConstraint("CK_Invoice_Total_NonNegative", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Invoices_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "IdRoom",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RoomId_BillingMonth",
                table: "Invoices",
                columns: new[] { "RoomId", "BillingMonth" },
                unique: true);
        }
    }
}
