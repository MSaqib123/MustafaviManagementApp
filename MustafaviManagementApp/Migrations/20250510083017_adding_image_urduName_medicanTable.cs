using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class adding_image_urduName_medicanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Medicines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrduName",
                table: "Medicines",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "UrduName",
                table: "Medicines");
        }
    }
}
