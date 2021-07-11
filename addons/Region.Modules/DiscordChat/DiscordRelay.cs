using Chris.OS.Additions.Utils;
using JNogueira.Discord.Webhook.Client;
using Mono.Addins;
using Newtonsoft.Json;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordChat
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordChat")]

    class DiscordRelay : EmptyModule
    {
        private String m_discordWebhookURL = null;

        public override String Name
        {
            get
            {
                return "DiscordRelay";
            }
        }

        public override void Initialise(IConfigSource source)
        {
            if (base.Config == null)
            {
                base.Config = source;

                if (base.Config.Configs["Network"] != null)
                    m_discordWebhookURL = base.Config.Configs["Network"].GetString("discord", null);
            }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.EventManager.OnChatFromClient += onChat;
            base.World.EventManager.OnMakeRootAgent += newRootAgent;
        }

        private void newRootAgent(ScenePresence obj)
        {
            sendMessage(base.World.Name, obj.Name + " has entered the region.");
        }

        private void onChat(object sender, OSChatMessage chat)
        {
            if (chat.Channel == 0)
            {
                sendMessage(chat.From, chat.Message);
            }
        }

        private async void sendMessage(String name, String message)
        {
            if (m_discordWebhookURL == null)
                return;

            DiscordWebhookClient client = new DiscordWebhookClient(m_discordWebhookURL);

            DiscordMessage discordMessage = new DiscordMessage(message, name);
            await client.SendToDiscord(discordMessage);
        }
    }
}
