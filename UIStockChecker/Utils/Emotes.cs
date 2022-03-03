using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIStockChecker.Database;

namespace UIStockChecker.Utils
{
    public static class Emotes
    {
        public const long UBIQUITI_CHANNEL = 944791303017873418;

        public static List<Emote> GetValidEmotesForReactions()
        {
            var emotes = new List<Emote>();

            using (var db = new ItemContext())
            {
                db.Items.ToList().Where(b => b.IgnoreItem == false).ToList().ForEach(item =>
                {
                    emotes.Add(Program.GetDiscordClient().Guilds
                        .SelectMany(x => x.Emotes)
                        .FirstOrDefault(x => x.Name.IndexOf(
                        item.Name.ToString().Replace(" ", "").Replace("\"", "").Replace(")", "").Replace("(", ""), StringComparison.OrdinalIgnoreCase) != -1));
                });
            }

            return emotes;
        }

    }
}
