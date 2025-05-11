using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeafLoop.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerAndSellerConfirmationToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BuyerConfirmed",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SellerConfirmed",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerConfirmed",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SellerConfirmed",
                table: "Transactions");
        }
    }
}
