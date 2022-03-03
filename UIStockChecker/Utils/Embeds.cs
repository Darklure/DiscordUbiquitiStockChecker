using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace UIStockChecker.Utils
{
    public static class Embeds
    {
        public const string EMBED_URL = @"https://www.ui.com/";
        public const string EMBED_THUMBNAIL_URL = @"https://cdn.iconscout.com/icon/free/png-256/ubiquiti-2752044-2284861.png";

        public static EmbedBuilder EmbedBuilderBot(string title, List<EmbedFieldBuilder> fields, string imageUrl)
        {
            var eb = new EmbedBuilder()
            {
                Color = Discord.Color.Blue,
                Title = title,
                Url = EMBED_URL,
                ThumbnailUrl = imageUrl.Length == 0 ? EMBED_THUMBNAIL_URL : imageUrl,
                Fields = fields,
                ImageUrl = ""
            };

            return eb;
        }

    }
}
