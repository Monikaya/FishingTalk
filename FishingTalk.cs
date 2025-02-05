using Cove.Server.Plugins;
using Cove.Server.Actor;
using Cove.Server.Utils;
using Cove.Server;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Net; //For webclient
using System.Collections.Specialized;
using Newtonsoft.Json;

// Change the namespace and class name!
namespace FishingTalk
{
    public class TalkConfig
    {
        [JsonProperty("token")]
        public required string Token { get; set; }
        [JsonProperty("webhookurl")]
        public required string WebhookURL { get; set; }
        public required string WebhookChannelId { get; set; }
        public required string WebhookUserId { get; set; }
        public string IconURL { get; set; }
    }

    public class WebhookRequest
    {
        [JsonProperty("application_id")]
        public string ApplicationId { get; set; }
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }
        [JsonProperty("guild_id")]
        public string GuildId { get; set; }
        [JsonProperty("id")]
        public string WebhookId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public int type { get; set; }
        [JsonProperty("token")]
        public string token { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class FishingTalk : CovePlugin
    {
        private TalkConfig talkConfig;

        private DiscordBot _discordBot;

        public FishingTalk(CoveServer server) : base(server)
        { 
            _discordBot = new DiscordBot(this);
        }

        public override async void onInit()
        {
            base.onInit();

            Log("Initializing FishingTalk!");

            string json = File.ReadAllText("fishingtalk.json");
            talkConfig = JsonConvert.DeserializeObject<TalkConfig>(json);

            WebhookRequest webhookJson = await WebGET(talkConfig.WebhookURL);

            if(talkConfig.IconURL == null)
            {
                talkConfig.IconURL = "https://i.imgur.com/5pQ9KKr.png";
            }

            talkConfig.WebhookChannelId = webhookJson.ChannelId;
            talkConfig.WebhookUserId = webhookJson.WebhookId;

            _ = _discordBot.MainAsync(talkConfig);

            Log("FishingTalk working!");

        }

        public override void onChatMessage(WFPlayer sender, string message)
        {
            base.onChatMessage(sender, message);
            sendDiscordWebhook(talkConfig.WebhookURL, talkConfig.IconURL, sender.Username, message);
        }

        public override void onPlayerJoin(WFPlayer player)
        {
            base.onPlayerJoin(player);
            Dictionary<string, string> config = ConfigReader.ReadConfig("server.cfg");
            var allPlayers = GetAllPlayers();
            int maxPlayers = int.Parse(config["maxPlayers"]);
            string message = player.Username + " joined. [" + allPlayers.Length + "/" + maxPlayers + "]";
            sendDiscordWebhook(talkConfig.WebhookURL, talkConfig.IconURL, "Server", message);
        }

        public override void onPlayerLeave(WFPlayer player)
        {
            base.onPlayerJoin(player);
            Dictionary<string, string> config = ConfigReader.ReadConfig("server.cfg");
            var allPlayers = GetAllPlayers();
            int maxPlayers = int.Parse(config["maxPlayers"]);
            string message = player.Username + " left. [" + allPlayers.Length + "/" + maxPlayers + "]";
            sendDiscordWebhook(talkConfig.WebhookURL, talkConfig.IconURL, "Server", message);
        }

        public static void sendDiscordWebhook(string URL, string profilepic, string username, string message)
        {
                NameValueCollection discordValues = new NameValueCollection();
                discordValues.Add("username", username);
                discordValues.Add("avatar_url", profilepic);
                discordValues.Add("content", message);
                new WebClient().UploadValues(URL, discordValues);
        }

        public void SendWebfishMessage(string username, string message)
        {
            Console.WriteLine(username + ": " + message);
            SendGlobalChatMessage(username + ": " + message);
        }
        static async Task<WebhookRequest> WebGET(string url)
        {
            using HttpClient client = new HttpClient();
            string request = await client.GetStringAsync(url);
            WebhookRequest json = JsonConvert.DeserializeObject<WebhookRequest>(request);
            return json;
        }
    }
    public class DiscordBot
    {
        private DiscordSocketClient _client;
        private FishingTalk _plugin;
        private TalkConfig talkConfig;

        public DiscordBot(FishingTalk plugin)
        {
            _plugin = plugin;
        }


        public async Task MainAsync(TalkConfig importConfig)
        {
            var config = new DiscordSocketConfig {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent, // Add MessageContent here
                // ... other config ...
            };
            _client = new DiscordSocketClient(config);

            _client.Log += Log; // For logging events (optional but recommended)

            talkConfig = importConfig;

            await _client.LoginAsync(TokenType.Bot, talkConfig.Token);
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
            if (message.Author.Id == _client.CurrentUser.Id || message.Author.Id.ToString().Equals(talkConfig.WebhookUserId)) return;

            // Only see msgs in chat-stream channel
            if (!message.Channel.Id.ToString().Equals(talkConfig.WebhookChannelId)) return;

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