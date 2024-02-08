using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PayoutUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Payouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_UserId",
                table: "Payouts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payouts_AspNetUsers_UserId",
                table: "Payouts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payouts_AspNetUsers_UserId",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_UserId",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Payouts");
        }
    }
}
