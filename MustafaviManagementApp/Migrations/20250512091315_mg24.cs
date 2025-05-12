using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class mg24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryParentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryParentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryParentCategories_Categorys_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categorys",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryParentCategories_ParentCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ParentCategories",
                        principalColumn: "ParentCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryParentCategories_CategoryId",
                table: "CategoryParentCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryParentCategories_ParentCategoryId",
                table: "CategoryParentCategories",
                column: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryParentCategories");
        }
    }
}
