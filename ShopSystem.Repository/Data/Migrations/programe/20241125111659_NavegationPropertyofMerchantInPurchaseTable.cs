using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopSystem.Repository.Data.Migrations.programe
{
    /// <inheritdoc />
    public partial class NavegationPropertyofMerchantInPurchaseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Merchants_MerchantId",
                table: "Purchases");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Merchants_MerchantId",
                table: "Purchases",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Merchants_MerchantId",
                table: "Purchases");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Merchants_MerchantId",
                table: "Purchases",
                column: "MerchantId",
                principalTable: "Merchants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
