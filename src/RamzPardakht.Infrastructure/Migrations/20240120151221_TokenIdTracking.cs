using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TokenIdTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTokenId",
                table: "ReferenceTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByTokenId",
                table: "ReferenceTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByTokenId",
                table: "ReferenceTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTokenId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByTokenId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByTokenId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedByTokenId",
                table: "Payments",
                column: "CreatedByTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ReferenceTokens_CreatedByTokenId",
                table: "Payments",
                column: "CreatedByTokenId",
                principalTable: "ReferenceTokens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ReferenceTokens_CreatedByTokenId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CreatedByTokenId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedByTokenId",
                table: "ReferenceTokens");

            migrationBuilder.DropColumn(
                name: "DeletedByTokenId",
                table: "ReferenceTokens");

            migrationBuilder.DropColumn(
                name: "ModifiedByTokenId",
                table: "ReferenceTokens");

            migrationBuilder.DropColumn(
                name: "CreatedByTokenId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedByTokenId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ModifiedByTokenId",
                table: "Payments");
        }
    }
}
