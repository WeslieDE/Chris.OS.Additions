using Chris.OS.Additions.Utils;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordRelay
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordRelay")]
    class DiscordRelay : EmptyModule
    {
        private String m_token = null;
        private ulong m_channel = 0;

        private String m_discordWebHookURL = null;
        private Boolean m_scriptChat = false;
        private Dictionary<UUID, String> m_nameCache = new Dictionary<UUID, String>();

        private static DiscordClient m_client = null;

        #region EmptyModule
        public override string Name
        {
            get { return "DiscordRelay"; }
        }

        public override void Initialise(IConfigSource source)
        {
            if (Config == null)
                Config = source;

            if (base.Config.Configs["Discord"] != null)
            {
                m_token = base.Config.Configs["Discord"].GetString("Token", null);
                m_channel = ulong.Parse(base.Config.Configs["Discord"].GetString("Channel", "0"));

                m_discordWebHookURL = base.Config.Configs["Discord"].GetString("WebHookURL", null);
                m_scriptChat = base.Config.Configs["Discord"].GetBoolean("ScriptChat", m_scriptChat);
            }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;
            base.World.EventManager.OnChatFromClient += onChat;
            base.World.EventManager.OnChatFromWorld += onChat;

            base.World.EventManager.OnNewPresence += scriptevent_OnNewPresence;
            base.World.EventManager.OnRemovePresence += scriptevent_OnRemovePresence;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osSendToDiscord");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }


            WebHook webhook = new WebHook(m_discordWebHookURL);
            webhook.Name = base.World.Name;
            webhook.Message = "The region started successfully.";
            webhook.sendAsync();

            RunBotAsync().GetAwaiter().GetResult();
        }

        public override void Close()
        {
            WebHook webhook = new WebHook(m_discordWebHookURL);
            webhook.Name = base.World.Name;
            webhook.Message = "The region will now be stopped.";
            webhook.sendAsync();
        }
        #endregion

        #region Events

        private void scriptevent_OnRemovePresence(UUID agentId)
        {
            String name = null;

            if (m_nameCache.TryGetValue(agentId, out name))
            {
                m_nameCache.Remove(agentId);

                WebHook webhook = new WebHook(m_discordWebHookURL);
                webhook.Name = base.World.Name;
                webhook.Message = "`" + name + "` has left the region.";
                webhook.sendAsync();
            }
        }

        private void scriptevent_OnNewPresence(ScenePresence presence)
        {
            if (presence.IsNPC)
                return;

            if (!presence.IsChildAgent)
                return;

            m_nameCache.Add(presence.UUID, presence.Name);

            WebHook webhook = new WebHook(m_discordWebHookURL);
            webhook.Name = base.World.Name;
            webhook.Message = "`" + presence.Name + "` has entered the region.";
            webhook.sendAsync();
        }

        private void onChat(object sender, OSChatMessage chat)
        {
            if (m_discordWebHookURL == null)
                return;

            if (chat.Channel == 0)
            {
                if (chat.Sender == null && m_scriptChat == false)
                    return;

                String senderName = null;

                if (senderName == null)
                {
                    ScenePresence presence = base.World.GetScenePresence(chat.SenderUUID);
                    if (presence != null)
                        senderName = presence.Name;
                }

                if (senderName == null)
                {
                    SceneObjectPart part = base.World.GetSceneObjectPart(chat.SenderUUID);
                    if (part != null)
                        senderName = part.Name;
                }

                if (senderName != null)
                {
                    WebHook webhook = new WebHook(m_discordWebHookURL);
                    webhook.Name = senderName + " @ " + base.World.Name;
                    webhook.Message = chat.Message;
                    webhook.sendAsync();
                }
            }
        }
        #endregion

        #region ScriptCommands
        public void osSendToDiscord(UUID hostID, UUID scriptID, String message)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            WebHook webhook = new WebHook(m_discordWebHookURL);
            webhook.Name = part.Name + " @ " + base.World.Name;
            webhook.Message = message;
            webhook.sendAsync();

            Thread.Sleep(250);
        }
        #endregion

        #region DSharpPlus
        public async Task RunBotAsync()
        {
            if (m_token == null)
                return;

            if (m_channel != 0)
                return;

            if (m_client != null)
                return;

            var discordConfig = new DiscordConfiguration
            {
                Token = m_token,
                TokenType = TokenType.Bot,

                AutoReconnect = true
            };

            // then we want to instantiate our client
            m_client = new DiscordClient(discordConfig);

            // next, let's hook some events, so we know
            m_client.MessageCreated += newMessage;

            // finally, let's connect and log in
            await m_client.ConnectAsync();

            // and this is to prevent premature quitting
            await Task.Delay(-1);
        }

        private Task newMessage(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.Id != m_channel)
                return Task.CompletedTask;

            IWorldComm wComm = base.World.RequestModuleInterface<IWorldComm>();

            wComm.DeliverMessage(ChatTypeEnum.Region, 0, e.Author.Username, UUID.Random(), e.Message.Content);

            return Task.CompletedTask;
        }
        #endregion
    }
}
