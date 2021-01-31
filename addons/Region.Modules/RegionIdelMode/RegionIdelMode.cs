using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenSim.Region.Framework.Interfaces;
using System.Timers;
using Mono.Addins;
using OpenSim.Framework;
using System.Threading;

namespace Chris.OS.Additions.Region.Modules.RegionIdelMode
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RegionIdelMode")]
    class RegionIdelMode : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Welt> m_scene = new List<Welt>();

        private List<System.Timers.Timer> m_timers = new List<System.Timers.Timer>();

        private IConfigSource m_config;

        private bool m_enable = false;

        private bool m_enableScripts = true;
        private bool m_enablePhys = true;
        private bool m_ScriptSoftMode = true;

        #region ISharedRegionModule
        public string Name
        {
            get { return "RegionIdelMode"; }
        }

        public void Initialise(IConfigSource config)
        {
            m_config = config;

            if (m_config.Configs["RegionIdelMode"] != null)
            {
                m_enable = m_config.Configs["RegionIdelMode"].GetBoolean("Enabled", m_enable);
                m_enableScripts = m_config.Configs["RegionIdelMode"].GetBoolean("Scripts", m_enableScripts);
                m_enablePhys = m_config.Configs["RegionIdelMode"].GetBoolean("Physics", m_enablePhys);
                m_ScriptSoftMode = m_config.Configs["RegionIdelMode"].GetBoolean("SoftMode", m_ScriptSoftMode);
            }

            if (m_enable == true)
                m_log.Info("[" + Name + "] DisablePhysic is enabled");

            if (m_enable == false)
                m_log.Info("[" + Name + "] DisablePhysic is disabled");
        }

        public void AddRegion(Scene scene)
        {
            if (m_enable == false)
                return;

            m_scene.Add(new Welt(scene));

            scene.EventManager.OnNewClient += newClient;

            System.Timers.Timer _phyTimer = new System.Timers.Timer(10000);
            _phyTimer.Elapsed += new ElapsedEventHandler(timerEvent);
            _phyTimer.Start();

            m_timers.Add(_phyTimer);
        }



        public void PostInitialise()
        {

        }

        public void Close()
        {
        }



        public Type ReplaceableInterface
        {
            get { return null; }
        }


        public void RegionLoaded(Scene scene)
        {
            
        }



        public void RemoveRegion(Scene scene)
        {
            //m_scene.Remove(scene);
        }


        #endregion

        #region Events
        public void timerEvent(object sender, ElapsedEventArgs e)
        {
            (new Thread(delegate () { changeState(); })).Start();
        }

        private void newClient(IClientAPI client)
        {
            (new Thread(delegate () { changeState(); })).Start();
        }
        #endregion

        #region Funktions
        private void changeState()
        {
            try
            {
                foreach (Welt _thisScene in m_scene)
                {
                    if (_thisScene.getRealAvatars() == 0 && _thisScene.LoginsEnabled == true)
                    {
                        if (_thisScene.ScriptsEnabled == true || _thisScene.PhysicsEnabled == true)
                        {
                            _thisScene.InUse = false;

                            if (m_enableScripts == true)
                            {
                                m_log.Info("[" + Name + "] Set region scripts in idle state");

                                if (m_ScriptSoftMode == true)
                                {

                                    if (_thisScene.ScriptsEnabled == true)
                                    {
                                        _thisScene.ScriptsEnabled = false;
                                    }
                                }
                                else
                                {
                                    _thisScene.ScriptsRunning = false;
                                }
                            }

                            if (m_enablePhys == true)
                            {
                                if (_thisScene.PhysicsEnabled == true)
                                {
                                    _thisScene.PhysicsEnabled = false;
                                    m_log.Info("[" + Name + "] Set region physics in idle state");
                                }
                            }

                            if (_thisScene.GetExtraSetting("auto_grant_pay_perms") != "true")
                                _thisScene.StoreExtraSetting("auto_grant_pay_perms", "true");
                        }
                    }
                    else
                    {
                        if (_thisScene.InUse == false)
                        {
                            _thisScene.InUse = true;

                            if (_thisScene.ScriptsEnabled == false || _thisScene.PhysicsEnabled == false)
                            {
                                if (m_enableScripts == true)
                                {
                                    m_log.Info("[" + Name + "] Wake up region scripts from idle state");

                                    if (m_ScriptSoftMode == true)
                                    {
                                        if (_thisScene.ScriptsEnabled == false)
                                        {
                                            _thisScene.ScriptsEnabled = true;
                                        }
                                    }
                                    else
                                    {
                                        _thisScene.ScriptsRunning = true;
                                    }
                                }

                                if (m_enablePhys == true)
                                {
                                    if (_thisScene.PhysicsEnabled == false)
                                    {
                                        _thisScene.PhysicsEnabled = true;
                                        m_log.Info("[" + Name + "] Wake up region physics from idle state");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception _error)
            {
                m_log.Error("[" + Name + "] FATAL ERROR: " + _error.Message);
                m_log.Error("[" + Name + "] FATAL ERROR: " + _error.StackTrace);

            }
        }
        #endregion
    }
}
