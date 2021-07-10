using Chris.OS.Additions.Utils;
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
        private String m_discordToken = null;

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
                    m_discordToken = base.Config.Configs["Network"].GetString("discord", null);
            }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World.EventManager.OnChatFromClient += onChat;
        }

        private void onChat(object sender, OSChatMessage chat)
        {
            if (m_discordToken == null)
                return;

            try
            {
                if (chat.Channel == 0)
                {
                    WebHookData data = new WebHookData();
                    data.content = chat.Message;
                    data.username = chat.From;

                    WebClient client = new WebClient();

                    string json = JsonConvert.SerializeObject(data);

                    client.UploadString(m_discordToken, json);
                }
            }
            catch
            {
                base.Logger.Error("Failed to send chat to discord.");
            }
        }
    }
}
