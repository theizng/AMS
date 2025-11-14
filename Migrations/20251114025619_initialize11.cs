using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Tenants",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_FullName",
                table: "Tenants",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PhoneNumber",
                table: "Tenants",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_FullName",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PhoneNumber",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Tenants",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
