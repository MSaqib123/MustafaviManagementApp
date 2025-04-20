using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MustafaviManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class adding_madican_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_MedicineId",
                table: "SaleDetails",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDetails_MedicineId",
                table: "PurchaseDetails",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_StoreId",
                table: "Medicines",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventorys_MedicineId",
                table: "Inventorys",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventorys_Medicines_MedicineId",
                table: "Inventorys",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Categorys_CategoryId",
                table: "Medicines",
                column: "CategoryId",
                principalTable: "Categorys",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Stores_StoreId",
                table: "Medicines",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "StoreId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseDetails_Medicines_MedicineId",
                table: "PurchaseDetails",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_Medicines_MedicineId",
                table: "SaleDetails",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventorys_Medicines_MedicineId",
                table: "Inventorys");

            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Categorys_CategoryId",
                table: "Medicines");

            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Stores_StoreId",
                table: "Medicines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseDetails_Medicines_MedicineId",
                table: "PurchaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_Medicines_MedicineId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_MedicineId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseDetails_MedicineId",
                table: "PurchaseDetails");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_StoreId",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Inventorys_MedicineId",
                table: "Inventorys");
        }
    }
}
