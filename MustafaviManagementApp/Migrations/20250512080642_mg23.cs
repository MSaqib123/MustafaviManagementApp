using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class mg23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "Categorys",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParentCategories",
                columns: table => new
                {
                    ParentCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentCategories", x => x.ParentCategoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorys_ParentCategoryId",
                table: "Categorys",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categorys_ParentCategories_ParentCategoryId",
                table: "Categorys",
                column: "ParentCategoryId",
                principalTable: "ParentCategories",
                principalColumn: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categorys_ParentCategories_ParentCategoryId",
                table: "Categorys");

            migrationBuilder.DropTable(
                name: "ParentCategories");

            migrationBuilder.DropIndex(
                name: "IX_Categorys_ParentCategoryId",
                table: "Categorys");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categorys");
        }
    }
}
