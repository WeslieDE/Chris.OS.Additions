using Chris.OS.Additions.Utils;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chris.OS.Additions.Region.Modules.DiscordRelay
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DiscordRelay")]
    class DiscordRelay : EmptyNonSharedModule
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

            if (chat.Channel == 0 && chat.Sender != null)
            {
                    WebHook webhook = new WebHook(m_discordWebHookURL);
                    webhook.Name = chat.From + " @ " + base.World.Name;
                    webhook.Message = chat.Message;
                    webhook.sendAsync();
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
    }
}
