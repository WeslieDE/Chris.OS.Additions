using Chris.OS.Additions.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using Mono.Addins;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using System;


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

        private void sendMessage(String name, String message)
        {
            if (m_discordWebhookURL == null)
                return;

            
        }
    }
}
