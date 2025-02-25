using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LetMasterWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AirtelRec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReconcileStatus",
                table: "ClientDebitRequests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReconcileStatus",
                table: "ClientDebitRequests");
        }
    }
}
