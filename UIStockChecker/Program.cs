using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UIStockChecker.Database;
using UIStockChecker.Models;
using UIStockChecker.Services;
using UIStockChecker.Utils;
using Color = Spectre.Console.Color;

namespace UIStockChecker
{
    class Program
    {
        private readonly IConfiguration _config;
        private static DiscordSocketClient _client;
        private static string _logLevel;
        private string cookie = "";

        public DateTime cookieLastUpdated = DateTime.Now;
        public string username = "";
        public string password = "";
        
        public static DiscordSocketClient GetDiscordClient()
        {
            return _client;
        }

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                _logLevel = args[0];
            }

            Console.Title = "Ubiquiti Stock Checker";

            AnsiConsole.Write(
                new FigletText("Ubiquiti Stock Checker")
                .LeftAligned()
                .Color(Color.Blue));

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json").Build();

            username = _config["Username"];
            password = _config["Password"];

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        private List<Item> UpdateDB(List<Item> items)
        {
            var data = new List<Item>();

            using (var db = new ItemContext())
            {
                var dbItems = db.Items.ToList();

                //Update existing items
                dbItems.Where(dataBaseItems => items.Any(webList => webList.Name.Equals(dataBaseItems.Name)))
                    .ToList().ForEach(dbItem => 
                    {
                        var webItem = items.Where(webList => webList.Name.Equals(dbItem.Name)).FirstOrDefault();
                        
                        if (webItem.InStock && !dbItem.InStock)
                        {
                            // if it is not in our DB as in stock, return it to show it is a new item added
                            data.Add(dbItem);
                        }

                        // Always update to current stock status
                        dbItem.InStock = webItem.InStock;
                        db.Update(dbItem);
                        
                    });

                // Add new item when found
                items.Where(webItems => !dbItems.Any(dataBaseItems => webItems.Name.Equals(dataBaseItems.Name)))
                    .ToList().ForEach(a =>
                    {
                        db.Add(a);
                        if (a.InStock)
                        {
                            data.Add(a);
                        }
                    });

                db.SaveChanges();

            }
            return data;
        }

        public List<Item> UpdateStock(List<Item> items)
        {            
            var itemsInStock = new List<Item>();
            var now = DateTime.Now;

            using (var db = new ItemContext())
            {
                var itemsDB = db.Items.ToList();
                var stocks = db.Stocks.ToList();
                
                foreach (var item in items)
                {
                    if (!item.InStock) continue;

                    var id = itemsDB.FindIndex(a => a.Name.Equals(item.Name));

                    var stock = new Stock 
                    { 
                        InStock = true, 
                        ItemId = itemsDB[id].Id, 
                        _date = now 
                    };

                    var result = stocks.FindLastIndex(x => x.ItemId == item.Id);

                    if (result == -1 || !stocks[result].InStock)
                    {
                        itemsInStock.Add(item);
                    }

                    db.Stocks.Add(stock);
                }

                db.SaveChanges();
            }

            return itemsInStock;
        }
        
        private void UpdateCookie()
        {
            if (cookie.Length == 0 || DateTime.Now.Subtract(cookieLastUpdated).TotalHours >= 12)
            {
                cookie = WebAccess.GetCookie(username, password);
                cookieLastUpdated = DateTime.Now;
                AnsiConsole.MarkupLine("[green]Cookie updated[/] - " + cookieLastUpdated.ToString());
            }
        }

        private List<Item> GetItemsFromUrls()
        {
            var items = new List<Item>();
            foreach (string url in WebAccess.GetUrls())
            {
                items.AddRange(WebAccess.ProcessURL(WebAccess.DownloadURL(url, cookie)));
            }

            return items.Distinct().ToList();
        }

        public void StockThread(Object o)
        {
            ulong id = 231906472043347968; // 3
            var channel = _client.GetChannel(id) as IMessageChannel; // 4
            if (channel != null)
            {
                UpdateCookie();

                var items = GetItemsFromUrls();

                if (items != null && items.Count > 0)
                {
                    var result = UpdateDB(items);
                    UpdateStock(items);

                    AnsiConsole.MarkupLine("[green][[" + DateTime.Now.ToString("HH:mm:ss") + "]] Stock Check - Completed Successfully.[/] Items found: " + items.Count());

                    var imageUrl = result.Count == 1 ? result[0].ImageUrl : "";

                    if (result != null && result.Count > 0)
                    {
                        var fields = new List<EmbedFieldBuilder>();

                        using (var db = new ItemContext())
                        {
                            ulong lastId = 1;

                            var headerField = new EmbedFieldBuilder()
                            {
                                IsInline = false,
                                Name = $"Items in stock",
                                Value = DateTime.Now
                            };

                            ulong userId = 0;

                            db.Subscribers.ToList().Where(a => result.Any(b => a.ItemId == b.Id)).ToList().ForEach(res => {

                                if (lastId == 1 || lastId != res.UserId)
                                {
                                    if (lastId != 1)
                                    {
                                        var privateChannel = channel.GetUserAsync(res.UserId).Result.CreateDMChannelAsync().Result;
                                        _ = privateChannel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker - Items in stock!", fields, "").Build()).Result;

                                        fields = new List<EmbedFieldBuilder>();
                                    }

                                    lastId = res.UserId;
                                }

                                var item = GetItemFromDb(res.ItemId);                                    

                                fields.Add(new EmbedFieldBuilder()
                                {
                                    IsInline = false,
                                    Name = item.Name,
                                    Value = item.Url
                                });
                                userId = res.UserId;
                            });
                            try
                            {
                                if (userId == 0) return;
                                var userIdChannel = channel.GetUserAsync(userId, CacheMode.AllowDownload).GetAwaiter().GetResult();
                                var privateChannel = userIdChannel.CreateDMChannelAsync().GetAwaiter().GetResult();
                                _ = privateChannel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker - Items in stock!", fields, "").Build()).Result;
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.WriteLine(ex.Message);
                            }

                        }
                    }
                    
                }
                else
                {
                    AnsiConsole.MarkupLine("[red][[" + DateTime.Now.ToString("HH:mm:ss") + "]] Stock Check Failed[/] - [bold]No items returned from website.[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red][[" + DateTime.Now.ToString("HH:mm:ss") + "]] Stock Check Failed[/] - [bold]No channel available in Discord.[/]");
            }
        }

        public Item GetItemFromDb(int id)
        {
            using(var db = new ItemContext())
            {
                return db.Items.ToList().Where(a => a.Id == id).FirstOrDefault();
            }
        }

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                _client = services.GetRequiredService<DiscordSocketClient>();
                services.GetRequiredService<LoggingService>();
                await _client.LoginAsync(TokenType.Bot, _config["Token"]);
                await _client.StartAsync();
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                var _timer = new Timer(StockThread, null, 0, 300000);

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"Connected as -> [{_client.CurrentUser}] :)");
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            var _discordConfig = new DiscordSocketConfig { 
                MessageCacheSize = 100, 
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            };
            _client = new DiscordSocketClient(_discordConfig);

            var services = new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton<DiscordSocketClient>(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<LoggingService>()
                .AddLogging(configure => configure.AddSerilog());

            if (!string.IsNullOrEmpty(_logLevel))
            {
                switch (_logLevel.ToLower())
                {
                    case "info":
                        {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
                            break;
                        }
                    case "error":
                        {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
                            break;
                        }
                    case "debug":
                        {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);
                            break;
                        }
                    default:
                        {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
                            break;
                        }
                }
            }
            else
            {
                services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
            }

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
