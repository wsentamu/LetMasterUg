using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetMasterWebApp.Migrations
{
    /// <inheritdoc />
    public partial class TxnModeUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantUnitId",
                table: "TenantUnitTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionMode",
                table: "TenantUnitTransactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TransactionRef",
                table: "TenantUnitTransactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantUnitId",
                table: "TenantUnitTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionMode",
                table: "TenantUnitTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionRef",
                table: "TenantUnitTransactions");
        }
    }
}
