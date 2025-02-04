﻿using Cove.Server.Plugins;
using Cove.Server.Actor;
using Cove.Server;
using Discord;
using Discord.WebSocket;
using System.Net; //For webclient
using System.Collections.Specialized;
using System.Text.Json; //For NameValueCollection

// Change the namespace and class name!
namespace CoveAutoMod
{
    public class CoveAutoMod : CovePlugin
    {
        private string webhook = "https://discord.com/api/webhooks/1336079280718741504/_siYI332sbBt1FcAAxUUZX6aS_wnImrcp3Gkfw6rFXk2gjvOFUXLw7pNvteFIZdPYRaX";
        public string icon = "https://i.imgur.com/5pQ9KKr.png";
        private DiscordBot _discordBot;

        public CoveAutoMod(CoveServer server) : base(server)
        { 
            _discordBot = new DiscordBot(this);
        }

        public override void onInit()
        {
            base.onInit();

            _ = _discordBot.MainAsync();

            Log("Hello world!");
        }

        public override void onChatMessage(WFPlayer sender, string message)
        {
            base.onChatMessage(sender, message);
            sendDiscordWebhook(webhook, icon, sender.Username, message);
        }

        public static void sendDiscordWebhook(string URL, string profilepic, string username, string message)
        {
                NameValueCollection discordValues = new NameValueCollection();
                discordValues.Add("username", username);
                discordValues.Add("avatar_url", profilepic);
                discordValues.Add("content", message);
                new WebClient().UploadValues(URL, discordValues);
        }

        public void SendWebfishMessage(string username, string message) {
            Console.WriteLine(username + ": " + message);
            SendGlobalChatMessage(username + ": " + message);
        }
    }
    public class DiscordBot
    {
        private DiscordSocketClient _client;
        private CoveAutoMod _plugin;

        public string guild;

        public DiscordBot(CoveAutoMod plugin)
        {
            _plugin = plugin;
        }

        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent, // Add MessageContent here
                // ... other config ...
            };
            _client = new DiscordSocketClient(config);

            _client.Log += Log; // For logging events (optional but recommended)

            // Replace with your bot's token!
            string token = "MTMzNjA5NTkyODE4Mzk0NzQwNg.GtVn0T.43Li60bfOI-SbkwtgnKWng9TxPE64ZnXlUlOF4";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            guild = _client.GetGuild();

            _client.MessageReceived += HandleMessage; // Event handler for incoming messages

            // Keep the program running (important!)
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleMessage(SocketMessage message)
        {
            // Ignore messages from the bot itself
            if (message.Author.Id == _client.CurrentUser.Id || message.Author.Id == 1336079280718741504) return;

            // Only see msgs in chat-stream channel
            if (message.Channel.Id != 1336079216038645780) return;

            // TODO: get guild somehow via _client.GetGuild() then use guild.GetUserAsync and use .Nickname to get their fun thing

            _plugin.SendWebfishMessage(message.Author.GlobalName, message.Content);
        }
    
    }
}