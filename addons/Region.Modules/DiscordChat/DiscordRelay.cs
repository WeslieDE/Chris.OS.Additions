using Chris.OS.Additions.Utils;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordChat
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordChat")]

    class DiscordRelay : EmptyModule
    {
        private String m_discordToken = null;
        private ulong m_channelID = 0;

        private static DiscordClient m_discordClient = null;
        private static DiscordChannel m_channel = null;

        public override String Name
        {
            get
            {
                return "DiscordRelay";
            }
        }

        public override void Initialise(IConfigSource source)
        {
            base.Config = source;

            if (base.Config.Configs["Discord"] != null)
                m_discordToken = base.Config.Configs["Discord"].GetString("token", null);

            if (base.Config.Configs["Discord"] != null)
                m_channelID = ulong.Parse(base.Config.Configs["Discord"].GetString("channelID", "0"));

            if (m_discordToken != null)
                ConnectDiscordBot().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.EventManager.OnChatFromClient += onChat;
            base.World.EventManager.OnMakeRootAgent += newRootAgent;
        }

        public override void Close()
        {
            if(m_discordClient != null)
            {
                m_discordClient.DisconnectAsync().Wait();
            }
        }

        private void sendMessage(String name, String message)
        {
            if (m_discordClient == null)
                return;

            if (m_channelID == 0)
                return;

            if (m_channel == null)
                m_channel = m_discordClient.GetChannelAsync(m_channelID).Result;

            m_channel.SendMessageAsync(name + ": " + message).Wait();

        }

        #region Events
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
        #endregion

        #region Discord

        async Task ConnectDiscordBot()
        {
            if (m_discordClient != null)
                return;

            DiscordConfiguration _configuration = new DiscordConfiguration { Token = m_discordToken, TokenType = TokenType.Bot };
            _configuration.AutoReconnect = true;
            _configuration.MinimumLogLevel = LogLevel.Information;
            m_discordClient = new DiscordClient(_configuration);

            m_discordClient.MessageCreated += onNewDiscordMessage;
            await m_discordClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task onNewDiscordMessage(DiscordClient sender, MessageCreateEventArgs e)
        {
            IWorldComm wComm = base.World.RequestModuleInterface<IWorldComm>();

            if(wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Region, 0, e.Author.Username, UUID.Random(), e.Message.Content);

            return null;
        }

        #endregion

    }
}
