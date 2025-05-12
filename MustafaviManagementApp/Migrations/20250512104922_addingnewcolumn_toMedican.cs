using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class addingnewcolumn_toMedican : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Medicines");

            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "Medicines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_ParentCategoryId",
                table: "Medicines",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_ParentCategories_ParentCategoryId",
                table: "Medicines",
                column: "ParentCategoryId",
                principalTable: "ParentCategories",
                principalColumn: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_ParentCategories_ParentCategoryId",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_ParentCategoryId",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Medicines");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Medicines",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
