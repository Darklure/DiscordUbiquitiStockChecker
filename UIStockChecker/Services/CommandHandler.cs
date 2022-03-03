using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UIStockChecker.Models;
using UIStockChecker.Utils;

namespace UIStockChecker.Services
{
    public class CommandHandler
    {
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly LoggingService _logging;

        public CommandHandler(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
            _logging = services.GetRequiredService<LoggingService>();

            // take action when we execute a command
            _commands.CommandExecuted += CommandExecutedAsync;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += MessageReceivedAsync;

            _client.ReactionAdded += ReactionAddedAsync;

            _client.ReactionRemoved += ReactionRemovedAsync;
        }

        public async Task InitializeAsync()
        {
            // register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> messageCache, Cacheable<IMessageChannel, ulong> channelCache, SocketReaction socketReaction)
        {
            var message = await messageCache.GetOrDownloadAsync();

            if ( socketReaction == null)
            {
                return;
            }

            var currentChannel = await channelCache.GetOrDownloadAsync();
            
            //Check to ensure we have a valid socket and are in the Ubiquiti channel
            if (socketReaction.Emote != null && socketReaction.UserId != 0 && currentChannel.Id == Emotes.UBIQUITI_CHANNEL)
            {
                var item = Item.GetProductItemFromEmote(socketReaction.Emote.Name);

                if (item.Name == null || item.Name.Length == 0)
                {
                    return;
                }

                Subscriber.AddSubscriber(socketReaction.UserId, item);
                var user = await currentChannel.GetUserAsync(socketReaction.UserId);

                if (user == null)
                {
                    user = await currentChannel.GetUserAsync(socketReaction.UserId);
                }

                var privateChannel = await user.CreateDMChannelAsync();

                var field = new EmbedFieldBuilder{ Name = "You have subscribed! ", Value = item.Name };

                await privateChannel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", new List<EmbedFieldBuilder> { field }, "").Build());
            }
        }

        //21510518651879424 removed ConnectDisplay7RecessedMount channel 944791303017873418
        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> messageCache, Cacheable<IMessageChannel, ulong> channelCache, SocketReaction socketReaction)
        {
            var message = await messageCache.GetOrDownloadAsync();

            if (socketReaction == null)
            {
                return;
            }

            var currentChannel = await channelCache.GetOrDownloadAsync();

            //Check to ensure we have a valid socket and are in the Ubiquiti channel
            if (socketReaction.Emote != null && socketReaction.UserId != 0 && currentChannel.Id == Emotes.UBIQUITI_CHANNEL)
            {
                var item = Item.GetProductItemFromEmote(socketReaction.Emote.Name);

                if (item.Name == null || item.Name.Length == 0)
                {
                    return;
                }                

                Subscriber.RemoveSubscriber(socketReaction.UserId, item);
                var user = await currentChannel.GetUserAsync(socketReaction.UserId);

                if (user == null)
                {
                    user = await currentChannel.GetUserAsync(socketReaction.UserId);
                }

                var privateChannel = await user.CreateDMChannelAsync();

                var field = new EmbedFieldBuilder { Name = "You have unsubscribed!", Value = item.Name };

                await privateChannel.SendMessageAsync("", false, Embeds.EmbedBuilderBot("Ubiquiti Stock Checker", new List<EmbedFieldBuilder> { field }, "").Build());
            }
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            // sets the argument position away from the prefix we set
            var argPos = 0;

            // get prefix from the configuration file
            char prefix = Char.Parse(_config["Prefix"]);

            // determine if the message has a valid prefix, and adjust argPos based on prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefix, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            // execute command if one is found that matches
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                await _logging.OnLogAsync(new LogMessage(LogSeverity.Warning, "CommandExecutedAsync", $"Failed to execute command [{command.Value}] for [{context.User.Username}] <-> [{result.ErrorReason}]"));
                return;
            }

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                await _logging.OnLogAsync(new LogMessage(LogSeverity.Info, "CommandExecutedAsync", $"Command [{command.Value.Name}] executed for -> [{context.User.Username}]"));
                return;
            }

            // failure scenario, let's let the user know
            //await context.Channel.SendMessageAsync($"Sorry, {context.User.Username}... something went wrong -> [{result}]!");
            await _logging.OnLogAsync(new LogMessage(LogSeverity.Info, "CommandExecutedAsync", $"{context.User.Username} -> something went wrong -> [{result}]!"));
        }
    }
}
