using Microsoft.EntityFrameworkCore.Migrations;

namespace UIStockChecker.Migrations
{
    public partial class AddedIgnoreItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreItem",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreItem",
                table: "Items");
        }
    }
}
