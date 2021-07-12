﻿using Chris.OS.Additions.Utils;
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
            base.World.EventManager.OnMakeRootAgent += onNewRootAgent;

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


        private void onNewRootAgent(ScenePresence obj)
        {
            //IWorldComm wComm = base.World.RequestModuleInterface<IWorldComm>();
            //wComm.DeliverMessageTo(obj.UUID, 0, new OpenMetaverse.Vector3(0, 0, 0), "Discord Relay", UUID.Random(), "On this region is the Chat discory relay active.");
        }

        private void onChat(object sender, OSChatMessage chat)
        {
            if (m_discordWebHookURL == null)
                return;

            if(chat.Channel == 0 && chat.Sender != null)
            {
                WebHook webhook = new WebHook(m_discordWebHookURL);
                webhook.Name = chat.From.Split('@')[0].Replace('.', ' ') + " @ " + base.World.Name;
                webhook.Message = chat.Message;
                webhook.sendAsync();
            }
        }
    }
}