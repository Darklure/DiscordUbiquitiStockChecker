using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using UIStockChecker.Database;

namespace UIStockChecker.Models
{
    public class Subscriber
    {
        [Key]
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public int ItemId { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastUpdated { get; set; }

        public static void AddSubscriber(ulong userId, Item item)
        {
            using (var db = new ItemContext())
            {
                var subscriber = db.Subscribers.ToList().Where(a => (ulong)a.UserId == userId && a.ItemId == item.Id).FirstOrDefault();
                if (subscriber != null && subscriber.Id > -1)
                {
                    subscriber.Enabled = true;
                    subscriber.LastUpdated = DateTime.Now;
                    db.Subscribers.Update(subscriber);
                }
                else
                {
                    db.Add(new Subscriber
                    {
                        Enabled = true,
                        UserId = userId,
                        ItemId = item.Id,
                        LastUpdated = DateTime.Now,
                    });
                }
                db.SaveChanges();
            }
        }

        public static void RemoveSubscriber(ulong userId, Item item)
        {
            using (var db = new ItemContext())
            {
                var subscriber = db.Subscribers.ToList().Where(a => (ulong)a.UserId == userId && a.ItemId == item.Id).FirstOrDefault();
                if (subscriber != null && subscriber.Id > -1)
                {
                    subscriber.Enabled = false;
                    subscriber.LastUpdated = DateTime.Now;
                    db.Subscribers.Update(subscriber);
                    db.SaveChanges();
                }
            }
        }

    }
}
