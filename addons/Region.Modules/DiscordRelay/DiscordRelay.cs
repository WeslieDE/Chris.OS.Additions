using Chris.OS.Additions.Utils;
using Mono.Addins;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordRelay
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordRelay")]
    class DiscordRelay : EmptyModule
    {
        private String m_discordWebHookURL = null;

        public override string Name
        {
            get { return "DiscordRelay"; }
        }

        public override void Initialise(IConfigSource source)
        {
            if (Config == null)
                Config = source;

            if (base.Config.Configs["Discord"] != null)
                m_discordWebHookURL = base.Config.Configs["Discord"].GetString("WebHookURL", null);

        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.EventManager.OnChatFromClient += onChat;
        }

        private void onChat(object sender, OSChatMessage chat)
        {
            if(chat.Channel == 0 && chat.Sender != null)
            {
                WebHook webhook = new WebHook(m_discordWebHookURL);
                webhook.Name = chat.From;
                webhook.Message = chat.Message;
            }
        }
    }
}
