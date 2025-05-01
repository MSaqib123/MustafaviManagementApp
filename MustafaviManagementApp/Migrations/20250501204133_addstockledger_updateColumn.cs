using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class addstockledger_updateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QtyBeforeChange",
                table: "StockLedgers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QtyCurrentlyAdded",
                table: "StockLedgers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QtyBeforeChange",
                table: "StockLedgers");

            migrationBuilder.DropColumn(
                name: "QtyCurrentlyAdded",
                table: "StockLedgers");
        }
    }
}
