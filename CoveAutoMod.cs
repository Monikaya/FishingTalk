﻿using Cove.Server.Plugins;
using Cove.Server;
using Cove.Server.Actor;
using System.Net; //For webclient
using System.Collections.Specialized; //For NameValueCollection

// Change the namespace and class name!
namespace CoveAutoMod
{
    public class CoveAutoMod : CovePlugin
    {
        private string webhook = "https://discord.com/api/webhooks/1336079280718741504/_siYI332sbBt1FcAAxUUZX6aS_wnImrcp3Gkfw6rFXk2gjvOFUXLw7pNvteFIZdPYRaX";
        public string icon = "https://i.imgur.com/5pQ9KKr.png";

        public CoveAutoMod(CoveServer server) : base(server) { }

        public override void onInit()
        {
            base.onInit();

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

    }
}