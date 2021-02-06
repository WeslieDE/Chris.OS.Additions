using Chris.OS.Additions.Utils;
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

    public class OsTimer : EmptyModule
    {
        private Dictionary<UUID, Timer> m_timers = new Dictionary<UUID, Timer>();
        public static IScriptModule ScriptEngine;

        #region EmptyModule
        public override string Name
        {
            get { return "OsTimer"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osTimerStart");
                m_scriptModule.RegisterScriptInvocation(this, "osTimerStop");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }

            ScriptEngine = base.World.RequestModuleInterface<IScriptModule>();

            base.World.EventManager.OnScriptReset += onScriptReset;
            base.World.EventManager.OnStopScript += onScriptStop;
            base.World.EventManager.OnRemoveScript += onScriptRemove;
        }

        public override void RemoveRegion(Scene scene)
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