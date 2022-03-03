using System;
using System.ComponentModel.DataAnnotations;

namespace UIStockChecker.Models
{
    public class Stock
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public bool InStock { get; set; }
        public DateTime _date { get; set; }
    }
}
