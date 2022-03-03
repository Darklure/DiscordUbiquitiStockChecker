using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UIStockChecker.Models;

namespace UIStockChecker.Database
{
    public class ItemContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public string DbPath { get; }
        public ItemContext()
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);
            DbPath = System.IO.Path.Join(path, "ubiquiti.db");
        }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = DbPath };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            optionsBuilder.UseSqlite(connection);
        }
    }
}
