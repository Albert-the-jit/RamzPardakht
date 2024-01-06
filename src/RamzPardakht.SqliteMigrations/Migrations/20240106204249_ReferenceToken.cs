using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferenceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedOn = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedById = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletedOn = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_UserId",
                table: "ReferenceTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceTokens");
        }
    }
}
