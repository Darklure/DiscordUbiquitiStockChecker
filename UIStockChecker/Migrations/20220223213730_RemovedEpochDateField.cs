using Microsoft.EntityFrameworkCore.Migrations;

namespace UIStockChecker.Migrations
{
    public partial class RemovedEpochDateField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Stocks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Stocks",
                type: "TEXT",
                nullable: true);
        }
    }
}
