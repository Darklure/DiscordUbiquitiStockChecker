using Microsoft.EntityFrameworkCore.Migrations;

namespace UIStockChecker.Migrations
{
    public partial class AddedUserNameToSubscribers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Subscribers",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Subscribers");
        }
    }
}
