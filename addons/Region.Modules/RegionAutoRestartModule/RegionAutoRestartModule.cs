using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using System.Timers;
using Mono.Addins;
using OpenSim.Framework;
using System.Threading;
using System.Diagnostics;
using Chris.OS.Additions.Utils;
using System.Net;

namespace Chris.OS.Additions.Region.Modules.RegionAutoRestartModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RegionAutoRestartModule")]
    class RegionAutoRestartModule : EmptyNonSharedModule
    {
        private System.Timers.Timer m_timer;
        private int m_restartCounter = 0;
        private IConfigSource m_config;

        private int m_restartTime = 30;
        private String m_webhookURL = null;

        #region EmptyModule

        public override string Name
        {
            get { return "RegionAutoRestartModule"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;
        }

        public override void AddRegion(Scene scene)
        {
            if (!base.Enabled)
                return;

            base.World = scene;

            m_timer = new System.Timers.Timer(60000);
            m_timer.Elapsed += new ElapsedEventHandler(timerEvent);
            m_timer.Start();

            base.Logger.Info("[AutoRestart] Enable AutoRestart with a time of " + m_restartTime.ToString());
        }

        public override void Initialise(IConfigSource config)
        {
            m_config = config;

            if(m_config.Configs["AutoRestart"] != null)
            {
                m_restartTime = m_config.Configs["AutoRestart"].GetInt("Time", 30);
                base.Enabled = m_config.Configs["AutoRestart"].GetBoolean("Enabled", base.Enabled);

                m_webhookURL = m_config.Configs["AutoRestart"].GetString("WebhookURL", m_webhookURL);
            }
        }
        #endregion

        #region Functions
        public void timerEvent(object sender, ElapsedEventArgs e)
        {
            m_restartCounter++;

            if (m_restartCounter >= m_restartTime)
            {
                int agentCount = 0;

                if (base.World.GetRootAgentCount() != 0)
                {
                    List<ScenePresence> _pres = base.World.GetScenePresences();

                    foreach (ScenePresence _p in _pres)
                    {
                        if (_p.PresenceType == PresenceType.User)
                        {
                            agentCount++;
                        }
                    }
                }

                if (agentCount == 0)
                {
                    base.Logger.Warn("[AutoRestart] Restart/Shutdown Region.");

                    base.World.Backup(true);

                    if(m_webhookURL != null)
                    {
                        try
                        {
                            WebClient client = new WebClient();
                            client.DownloadString(m_webhookURL);
                        }
                        catch(Exception error)
                        {
                            base.Logger.Warn("[AutoRestart] Exception while do webhoook call: " + error.Message);

                        }
                    }

                    Thread.Sleep(2000);
                    Process.GetCurrentProcess().Kill();
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                }
                else
                {
                    m_restartCounter = m_restartCounter - 10;
                }
            }
        }
        #endregion
    }
}
