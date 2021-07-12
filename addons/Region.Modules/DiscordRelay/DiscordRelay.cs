using Chris.OS.Additions.Utils;
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
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordRelay
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordRelay")]
    class DiscordRelay : EmptyModule
    {
        private String m_discordWebHookURL = null;
        private Boolean m_scriptChat = false;
        private Dictionary<UUID, String> m_nameCache = new Dictionary<UUID, String>();

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

            WebHook webhook = new WebHook(m_discordWebHookURL);
            webhook.Name = base.World.Name;
            webhook.Message = "The region started successfully.";
            webhook.sendAsync();
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
                webhook.Message = name + " has leave the region.";
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
            webhook.Message = presence.Name + " has entered the region.";
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
    }
}
