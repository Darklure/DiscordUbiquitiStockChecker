using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using UIStockChecker.Database;

namespace UIStockChecker.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Price { get; set; }
        public bool InStock { get; set; }
        public string ImageUrl { get; set; }
        public bool IgnoreItem { get; set; }

        public static Item GetProductItemFromEmote(string emote)
        {
            var item = new Item();

            using (var db = new ItemContext())
            {
                db.Items.ToList().ForEach((Action<Item>)(itemLoop =>
                {
                    if (itemLoop.Name.ToString().Replace(" ", "").Replace("\"", "").Replace(")", "").Replace("(", "").Equals(emote))
                    {
                        item = itemLoop;
                    }
                }));

            }

            return item;
        }

    }
}
