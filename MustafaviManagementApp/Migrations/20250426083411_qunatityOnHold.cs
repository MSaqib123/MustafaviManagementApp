using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class qunatityOnHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedQty",
                table: "Inventorys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedQty",
                table: "Inventorys");
        }
    }
}
