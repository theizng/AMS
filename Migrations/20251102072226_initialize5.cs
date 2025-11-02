using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class initialize5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bikes_Tenants_OwnerId",
                table: "Bikes");

            migrationBuilder.CreateIndex(
                name: "IX_Bikes_Plate",
                table: "Bikes",
                column: "Plate");

            migrationBuilder.AddForeignKey(
                name: "FK_Bikes_Tenants_OwnerId",
                table: "Bikes",
                column: "OwnerId",
                principalTable: "Tenants",
                principalColumn: "IdTenant",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bikes_Tenants_OwnerId",
                table: "Bikes");

            migrationBuilder.DropIndex(
                name: "IX_Bikes_Plate",
                table: "Bikes");

            migrationBuilder.AddForeignKey(
                name: "FK_Bikes_Tenants_OwnerId",
                table: "Bikes",
                column: "OwnerId",
                principalTable: "Tenants",
                principalColumn: "IdTenant",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
