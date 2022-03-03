using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UIStockChecker.Database;
using UIStockChecker.Models;
using UIStockChecker.Utils;

namespace UIStockChecker.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private static DiscordSocketClient _client;
        private List<String> _validColors = new List<String>();
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;
        ItemContext _db = new ItemContext();
        CommandService _commandService;
        public Commands(IServiceProvider services)
        {
            _services = services;
            _config = services.GetRequiredService<IConfiguration>();    
            _client = Program.GetDiscordClient();
            _commandService = new CommandService();
        }

        [Command("help")]
        public async Task Help()
        {
            List<CommandInfo> commands = _commandService.Commands.ToList();
            EmbedBuilder embedBuilder = new EmbedBuilder();

            foreach (CommandInfo command in commands)
            {
                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                embedBuilder.AddField(command.Name, embedFieldText);
            }

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }

        [Command("info")]
        [Summary("Displays Bot Details")]
        public Task SayAsync()
        {
            string info = "Checks current in stock items on Ubiquiti's site.";
            return ReplyAsync(info);
        }

        [Command("filter")]
        [Summary("Adds a filter to item. 0 = not filtered, 1 = filtered")]
        public async Task FilterItem([Remainder] string args = null)
        {
            var fields = new List<EmbedFieldBuilder>();

            if (Context.User.Id != 221510518651879424)
            {
                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Access Denied.",
                    Value = Context.User.Username
                });

                await Context.Channel.SendMessageAsync("You do not have access to this command!", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
                AnsiConsole.MarkupLine(" [red]User (" + Context.User.Id + ") does not have access to command[/] - (Filter)");
                return;
            }
            
            var argList = DiscordCommands.SplitArgs(args);

            if (args == null || argList.Count() != 2)
            {
                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Invalid Args Passed to Command.",
                    Value = "Args count: " + (args == null ? 0 : argList.ToList().Count())
                });

                await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
                return;
            }
            
            if (!argList[0].Equals("0") && !argList[0].Equals("1"))
            {
                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "No boolean value found in first argument.",
                    Value = "Boolean value found: " + argList[0]
                });

                await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
                return;
            }

            using (var db = new ItemContext())
            {
                var item = db.Items.ToList().Where(a => a.Name.ToLower().Equals(argList[1].ToString().Replace("\"", "").ToLower())).FirstOrDefault();

                if (item == null)
                {
                    fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Item not found in database",
                        Value = "Item name provided: " + argList[1]
                    });

                    await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
                    AnsiConsole.MarkupLine(" [red]User (" + Context.User.Id + ") does not have access to command[/] - (Subscribe)");
                    return;
                }

                item.IgnoreItem = args[0].ToString().Equals("0") ? false : true;
                db.Update(item);
                db.SaveChanges();

                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Item filter has been applied",
                    Value = item.Name + (args[0].ToString().Equals("0") ? " is not being filtered." : " is being filtered.")
                });
            }

            await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
        }

        [Command("subscribe")]
        [Summary("Returns current the current items in stock")]
        public async Task Subscribe()
        {
            if (Context.User.Id != 221510518651879424)
            {
                AnsiConsole.MarkupLine(" [red]User (" + Context.User.Id + ") does not have access to command[/] - (Subscribe)");
                return;
            }

            string textToSend = "";
            var emotes = new List<Emote>();

            var dbItems = _db.Items.ToList().Where(a => !a.IgnoreItem).ToList();

            foreach(var item in dbItems)
            {
                var emote = _client.Guilds
                    .SelectMany(x => x.Emotes)
                    .FirstOrDefault(x => x.Name.IndexOf(
                    item.Name.ToString().Replace(" ", "").Replace("\"", "").Replace(")","").Replace("(", ""), StringComparison.OrdinalIgnoreCase) != -1);
                
                emotes.Add(emote);

                textToSend += emote + ": " + item.Name + "\n";
            }
            
            var message = await Context.Channel.SendMessageAsync(textToSend);
/*
            foreach (var emote in emotes) {
                await message.AddReactionAsync(emote);
            }     */       
        }

        [Command("subscriptions")]
        [Summary("Returns current the items the user is subscribed to for stock updates.")]
        public async Task Subscriptions()
        {
            var userId = Context.User.Id;
            var fields = new List<EmbedFieldBuilder>();
            var item = new Item();

            _db.Subscribers.ToList().Where(a => a.UserId == userId).ToList().ForEach(b => {

                item = _db.Items.ToList().Single(c => c.Id == b.ItemId);

                var emote = _client.Guilds
                    .SelectMany(x => x.Emotes)
                    .FirstOrDefault(x => x.Name.IndexOf(
                    item.Name.ToString().Replace(" ", "").Replace("\"", "").Replace(")", "").Replace("(", ""), StringComparison.OrdinalIgnoreCase) != -1);

                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = emote + ": " + item.Name,
                    Value = b.LastUpdated,
                });
            });
            await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, "").Build());
        }

        [Command("stock")]
        [Summary("Returns current the current items in stock")]
        public async Task GetInStock([Remainder] string args = null)
        {
            string imageUrl = "";
            var fields = new List<EmbedFieldBuilder>();
            var items = WebAccess.ProcessURL(WebAccess.DownloadURL("https://store.ui.com/collections/unifi-protect"));

            if (items != null)
            {
                var headerField = new EmbedFieldBuilder()
                {
                    IsInline = false,
                    Name = $"Items in stock",
                    Value = DateTime.Now
                };
                
                fields.Add(headerField);

                imageUrl = items.Where(a => a.InStock == true).Count() > 1 ? "" : items.Where(a => a.InStock == true).First().Name;

                foreach (var item  in items)
                {
                    if (!item.InStock) continue;

                    imageUrl = imageUrl.Length == 0 ? item.ImageUrl : "";

                    fields.Add(new EmbedFieldBuilder()
                    {
                        IsInline = false,
                        Name = item.Name,
                        Value = item.Url
                    });
                }
            }

            await Context.Channel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", fields, imageUrl).Build());
        }
    }
}