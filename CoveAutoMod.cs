using Cove.Server.Plugins;
using Cove.Server.Actor;
using Cove.Server;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Net; //For webclient
using System.Collections.Specialized;
using System;
using System.IO;
using Newtonsoft.Json;

// Change the namespace and class name!
namespace FishingTalk
{
    public class FishingTalk : CovePlugin
    {
        private string webhook = Environment.GetEnvironmentVariable("COVEWEBHOOKURL").ToString();

        public string icon = "https://i.imgur.com/5pQ9KKr.png";
        private DiscordBot _discordBot;

        public FishingTalk(CoveServer server) : base(server)
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
        private FishingTalk _plugin;

        public DiscordBot(FishingTalk plugin)
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

            string token = Environment.GetEnvironmentVariable("COVETOKEN").ToString();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            
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
            if (message.Author.Id == _client.CurrentUser.Id || message.Author.Id == 1336488747692457984) return;

            // Only see msgs in chat-stream channel
            if (message.Channel.Id != 1336079216038645780) return;

            SocketCommandContext context = new SocketCommandContext(_client, message as SocketUserMessage);

            SocketGuild guild = context.Guild;
            var author = guild.GetUser(message.Author.Id);
            string authorUsername = author?.Nickname ?? author.GlobalName;

            // Make it get token from token.txt via some method
            // also regenerate webhook url and do the same (it's compromised)

            _plugin.SendWebfishMessage(authorUsername, message.Content);
        }
    
    }
}