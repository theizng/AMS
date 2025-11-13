using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddendumNotifiedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddendums_AddendumNumber",
                table: "ContractAddendums",
                column: "AddendumNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddendums_ParentContractId",
                table: "ContractAddendums",
                column: "ParentContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractAddendums");

            migrationBuilder.DropColumn(
                name: "AddendumNotifiedAt",
                table: "Contracts");
        }
    }
}
