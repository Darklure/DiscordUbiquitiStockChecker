using System.ComponentModel.DataAnnotations;

namespace UIStockChecker.Models
{
    public class Settings
    {
        [Key]
        public int Id { get; set; }
        public int UpdateIntervalInMs { get; set; }

    }
}
