using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamzPardakht.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Payout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "UsdAmount",
                table: "Payments",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<double>(
                name: "PaidAmount",
                table: "Payments",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "Payments",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<double>(type: "NUMERIC", nullable: false),
                    Currency = table.Column<int>(type: "INTEGER", nullable: false),
                    NetworkFee = table.Column<double>(type: "NUMERIC", nullable: false),
                    ToAddress = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_ReferenceTokens_CreatedByTokenId",
                        column: x => x.CreatedByTokenId,
                        principalTable: "ReferenceTokens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayoutPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<double>(type: "NUMERIC", nullable: false),
                    PayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<long>(type: "INTEGER", nullable: false),
                    ModifiedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedByTokenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutPayments_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayoutPayments_Payouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "Payouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutPayments_PaymentId",
                table: "PayoutPayments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutPayments_PayoutId",
                table: "PayoutPayments",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_CreatedByTokenId",
                table: "Payouts",
                column: "CreatedByTokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutPayments");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.AlterColumn<decimal>(
                name: "UsdAmount",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaidAmount",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "NUMERIC");
        }
    }
}
