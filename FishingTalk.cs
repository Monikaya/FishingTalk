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
using System.Linq.Expressions;
using Discord.Interactions;

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
            if(message.Contains("@everyone")) message = "I tried to ateveryone! This message was replaced.";
            if(message.Contains("@here")) message = "I tried to athere! This message was replaced.";

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
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;

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

        public async Task Client_Ready()
        {
            var usersCommand = new SlashCommandBuilder()
                .WithName("users")
                .WithDescription("List the users currently playing.")
                .AddOption("page", ApplicationCommandOptionType.Integer, "The page we look at", isRequired: false);

            var kickCommand = new SlashCommandBuilder()
                .WithName("kick")
                .WithDescription("Kick a user from your Webfishing server.")
                .AddOption("user", ApplicationCommandOptionType.String, "The user who will be kicked", isRequired: true);

            var banCommand = new SlashCommandBuilder()
                .WithName("ban")
                .WithDescription("Ban a user from your Webfishing server.")
                .AddOption("user", ApplicationCommandOptionType.String, "The user who will be banned", isRequired: true);

            var banIDCommand = new SlashCommandBuilder()
                .WithName("banid")
                .WithDescription("Ban a user by their steam id")
                .AddOption("user-id", ApplicationCommandOptionType.String, "The steam id to ban", isRequired: true);
            
            await _client.CreateGlobalApplicationCommandAsync(usersCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(kickCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(banCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(banIDCommand.Build());
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch(command.Data.Name)
            {
                case "users":
                    await ListUsers(command);
                    break;
                case "kick":
                    await KickUser(command);
                    break;
                case "ban":
                    await BanUser(command);
                    break;
                case "banid":
                    await BanUserByID(command);
                    break;
            }
        }

        private async Task ListUsers(SocketSlashCommand command)
        {
            int pageNumber = (int)(long)command.Data.Options.First().Value;
            
            // this chunk is stolen directly from Dr.Meepso's Cove.ChatCommands. I'm sorry!
            
            int pageSize = 1;
            
            //if(argsPageNumber != 1) pageNumber = argsPageNumber; 

            var allPlayers = _plugin.GetAllPlayers();
            int totalPlayers = allPlayers.Length;
            int totalPages = (int)Math.Ceiling((double)totalPlayers / pageSize);
            // Ensure the page number is within the valid range
            if (pageNumber > totalPages) pageNumber = totalPages;
            // Get the players for the current page
            var playersOnPage = allPlayers.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            // Build the message to send
            string messageBody = "";
            foreach (var iPlayer in playersOnPage)
            {
                messageBody += $"\n{iPlayer.Username}: {iPlayer.FisherID}";
            }

            var embed = new EmbedBuilder()
                .WithTitle("Players Online")
                .WithDescription(messageBody)
                .WithColor(Color.DarkMagenta)
                .WithFooter("Page " + pageNumber + "/" + totalPages);

            await command.RespondAsync(embed: embed.Build());
        }

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        private async Task KickUser(SocketSlashCommand command)
        {
            SocketGuildUser user = command.User as SocketGuildUser;
            if(!user.GuildPermissions.KickMembers)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Not Allowed")
                    .WithColor(Color.Red)
                    .WithDescription("You don't have the permissison to access this command");

                await command.RespondAsync(embed: embed.Build());
                return;
            }

            string playerIdent = (string)command.Data.Options.First().Value;

            //stolen from meepso blatently

            // try find a user with the username first
            var AllPlayers = _plugin.GetAllPlayers();
            WFPlayer kickedplayer = AllPlayers.ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            // if there is no player with the username try find someone with that fisher ID
            if (kickedplayer == null)
                kickedplayer = AllPlayers.ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            if (kickedplayer == null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Not Found")
                    .WithColor(Color.Red)
                    .WithDescription("The player wasn't found.");
                await command.RespondAsync(embed: embed.Build());
            }
            else
            {
                _plugin.KickPlayer(kickedplayer);

                var embed = new EmbedBuilder()
                    .WithTitle("Player Kicked")
                    .WithColor(Color.Green)
                    .WithDescription("Sucessfully kicked " + kickedplayer.Username);
                
                await command.RespondAsync(embed: embed.Build());
            }
        }
        
        [DefaultMemberPermissions(GuildPermission.BanMembers)]
        private async Task BanUser(SocketSlashCommand command)
        {
            SocketGuildUser user = command.User as SocketGuildUser;
            if(!user.GuildPermissions.BanMembers)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Not Allowed")
                    .WithColor(Color.Red)
                    .WithDescription("You don't have the permissison to access this command");

                await command.RespondAsync(embed: embed.Build());
                return;
            }

            string playerIdent = (string)command.Data.Options.First().Value;

            // hacky fix, ALSO STOLE M FROM MEEPSO I SORRY
            // Extract player name from the command message
            // try find a user with the username first
            var AllPlayers = _plugin.GetAllPlayers();
            WFPlayer playerToBan = AllPlayers.ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            // if there is no player with the username try find someone with that fisher ID
            if (playerToBan == null)
                playerToBan = AllPlayers.ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
            if (playerToBan == null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Not Found")
                    .WithColor(Color.Red)
                    .WithDescription("The player wasn't found.");
                await command.RespondAsync(embed: embed.Build());
            }
            else
            {
                _plugin.BanPlayer(playerToBan);

                var embed = new EmbedBuilder()
                    .WithTitle("Player Banned")
                    .WithColor(Color.Green)
                    .WithDescription("Sucessfully banned " + playerToBan.Username);
                
                await command.RespondAsync(embed: embed.Build());
            }
        }

        [DefaultMemberPermissions(GuildPermission.BanMembers)]
        private async Task BanUserByID(SocketSlashCommand command)
        {
            SocketGuildUser user = command.User as SocketGuildUser;
            if(!user.GuildPermissions.BanMembers)
            {
                var embedPP = new EmbedBuilder()
                    .WithTitle("Not Allowed")
                    .WithColor(Color.Red)
                    .WithDescription("You don't have the permissison to access this command");

                await command.RespondAsync(embed: embedPP.Build());
                return;
            }
            long playerID;
            try
            {
                playerID = long.Parse((string)command.Data.Options.First().Value);
                //playerID = (long)command.Data.Options.First().Value;
            }
            catch
            {
                var embedPP = new EmbedBuilder()
                    .WithTitle("Not a String")
                    .WithColor(Color.Red)
                    .WithDescription("The Steam id is probably not an integer");

                await command.RespondAsync(embed: embedPP.Build());
                return;   
            }

            File.AppendAllText("bans.txt", playerID + " #hello gaming" + Environment.NewLine);
                
            var embed = new EmbedBuilder()
                .WithTitle("Player Banned")
                .WithColor(Color.Green)
                .WithDescription("Sucessfully banned " + playerID);
            
            await command.RespondAsync(embed: embed.Build());
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
            string authorUsername = author?.Nickname ?? author.GlobalName ?? author.DisplayName;

            // Make it get token from token.txt via some method
            // also regenerate webhook url and do the same (it's compromised)

            _plugin.SendWebfishMessage(authorUsername, message.Content);
        }
    
    }
}