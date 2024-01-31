using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SuccessUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CallbackUrl",
                table: "Payments",
                newName: "SuccessUrl");

            migrationBuilder.AddColumn<string>(
                name: "CancelUrl",
                table: "Payments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelUrl",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "SuccessUrl",
                table: "Payments",
                newName: "CallbackUrl");
        }
    }
}
