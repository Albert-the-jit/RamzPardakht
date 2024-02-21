using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Archive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Archives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileExtension = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<long>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedById = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeletedOn = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archives", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archives");
        }
    }
}
