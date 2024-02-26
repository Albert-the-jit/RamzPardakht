using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceTokenLogoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LogoId",
                table: "ReferenceTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_LogoId",
                table: "ReferenceTokens",
                column: "LogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTokens_Archives_LogoId",
                table: "ReferenceTokens",
                column: "LogoId",
                principalTable: "Archives",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTokens_Archives_LogoId",
                table: "ReferenceTokens");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceTokens_LogoId",
                table: "ReferenceTokens");

            migrationBuilder.DropColumn(
                name: "LogoId",
                table: "ReferenceTokens");
        }
    }
}
