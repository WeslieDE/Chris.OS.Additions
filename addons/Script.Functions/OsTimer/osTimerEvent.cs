using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;

[assembly: Addin("osTimerEvent", "0.1")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]
namespace OpenSim.TimerThread
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "osTimerEvent")]

    class osTimerEvent : INonSharedRegionModule
    {
        private Dictionary<UUID, Timer> m_timers = new Dictionary<UUID, Timer>();

        private Scene m_scene = null;

        public static IScriptModule ScriptEngine;

        #region INonSharedRegionModule
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "osTimerEvent"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {

        }

        public void Close()
        {

        }

        public void Initialise(IConfigSource source)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            m_scene = scene;

            try
            {
                IScriptModuleComms m_scriptModule = m_scene.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "os_startTimer");
                m_scriptModule.RegisterScriptInvocation(this, "os_stopTimer");
                m_scriptModule.RegisterScriptInvocation(this, "osTimerStart");
                m_scriptModule.RegisterScriptInvocation(this, "osTimerStop");

            }
            catch (Exception e)
            {
                m_log.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }

            ScriptEngine = m_scene.RequestModuleInterface<IScriptModule>();

            m_scene.EventManager.OnScriptReset += onScriptReset;
            m_scene.EventManager.OnStopScript += onScriptStop;
            m_scene.EventManager.OnRemoveScript += onScriptRemove;
        }

        public void RemoveRegion(Scene scene)
        {
            foreach (Timer timer in m_timers.Values)
            {
                timer.Stop();
            }
        }
        #endregion

        #region OpenSimulatorEvents

        private void onScriptRemove(uint localID, UUID itemID)
        {
            os_stopTimer(UUID.Zero, itemID);
        }

        private void onScriptStop(uint localID, UUID itemID)
        {
            os_stopTimer(UUID.Zero, itemID);
        }

        private void onScriptReset(uint localID, UUID itemID)
        {
            os_stopTimer(UUID.Zero, itemID);
        }

        #endregion

        #region Script functions

        [ScriptInvocation]
        public void osTimerStart(UUID hostID, UUID scriptID, float time)
        {
            os_startTimer(hostID, scriptID, time);
        }

        [ScriptInvocation]
        public void osTimerStop(UUID hostID, UUID scriptID)
        {
            os_stopTimer(hostID, scriptID);
        }

        [ScriptInvocation]
        public void os_startTimer(UUID hostID, UUID scriptID, float time)
        {
            if (time == 0)
            {
                os_stopTimer(hostID, scriptID);
                return;
            }

            Timer timer = null;

            if (m_timers.TryGetValue(scriptID, out timer))
            {
                //Existing Timer
                timer.Stop();
                timer.Interval = time * 1000;
                timer.Start();
            }
            else
            {
                //New Timer
                timer = new Timer(scriptID);
                timer.AutoReset = false;
                timer.Elapsed += timer.run;
                timer.Interval = time * 1000;
                timer.Start();
                m_timers.Add(scriptID, timer);
            }
        }

        [ScriptInvocation]
        public void os_stopTimer(UUID hostID, UUID scriptID)
        {
            Timer timer = null;
            if (m_timers.TryGetValue(scriptID, out timer))
            {
                timer.Stop();
                m_timers.Remove(scriptID);
                timer.Dispose();
            }
        }
        #endregion
    }
}
