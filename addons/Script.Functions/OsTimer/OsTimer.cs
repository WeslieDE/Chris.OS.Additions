using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.OsTimer
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OsTimer")]

    class OsTimer : INonSharedRegionModule
    {
        private Dictionary<UUID, Timer> m_timers = new Dictionary<UUID, Timer>();

        private Scene m_scene = null;

        public static IScriptModule ScriptEngine;

        #region INonSharedRegionModule
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "OsTimer"; }
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
            osTimerStop(UUID.Zero, itemID);
        }

        private void onScriptStop(uint localID, UUID itemID)
        {
            osTimerStop(UUID.Zero, itemID);
        }

        private void onScriptReset(uint localID, UUID itemID)
        {
            osTimerStop(UUID.Zero, itemID);
        }

        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osTimerStart(UUID hostID, UUID scriptID, float time)
        {
            if (time == 0)
            {
                osTimerStop(hostID, scriptID);
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
        public void osTimerStop(UUID hostID, UUID scriptID)
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
