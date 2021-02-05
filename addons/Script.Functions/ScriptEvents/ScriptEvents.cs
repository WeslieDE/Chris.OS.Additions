using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.ScriptEvents
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ScriptEvents")]

    class ScriptEvents : INonSharedRegionModule
    {
        private List<UUID> m_listener = new List<UUID>();

        private Scene m_scene = null;

        public static IScriptModule ScriptEngine;

        #region INonSharedRegionModule
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "ScriptEvents"; }
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

                m_scriptModule.RegisterScriptInvocation(this, "osStartScriptEvents");
                m_scriptModule.RegisterScriptInvocation(this, "osStopScriptEvents");
                m_scriptModule.RegisterScriptInvocation(this, "osTriggerCustomEvent");

                m_scriptModule.RegisterConstant("EVENT_CUSTOM", 0);
                m_scriptModule.RegisterConstant("EVENT_NEWPRESENCE", 1);
                m_scriptModule.RegisterConstant("EVENT_REMOVEPRESENCE", 2);
                m_scriptModule.RegisterConstant("EVENT_AVATARENTERPARCEL", 3);
                m_scriptModule.RegisterConstant("EVENT_LINKSETMOVE", 4);

                m_log.Info("[" + Name + "]: Successfully registerd all script methods.");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[" + Name + "]: Script method registration failed; {0}", e.Message);
            }

            ScriptEngine = m_scene.RequestModuleInterface<IScriptModule>();

            m_scene.EventManager.OnScriptReset += onScriptStart;
            m_scene.EventManager.OnScriptReset += onScriptReset;
            m_scene.EventManager.OnStopScript += onScriptStop;
            m_scene.EventManager.OnRemoveScript += onScriptRemove;

            //Events for Scripts
            m_scene.EventManager.OnNewPresence += scriptevent_OnNewPresence;
            m_scene.EventManager.OnRemovePresence += scriptevent_OnRemovePresence;
            m_scene.EventManager.OnAvatarEnteringNewParcel += scriptevent_OnAvatarEnteringNewParcel;
            m_scene.EventManager.OnSceneGroupMove += scriptevent_OnSceneGroupMove;
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.EventManager.OnScriptReset -= onScriptReset;
            m_scene.EventManager.OnStopScript -= onScriptStop;
            m_scene.EventManager.OnRemoveScript -= onScriptRemove;

            //Events for Scripts
            m_scene.EventManager.OnNewPresence -= scriptevent_OnNewPresence;
            m_scene.EventManager.OnRemovePresence -= scriptevent_OnRemovePresence;
            m_scene.EventManager.OnAvatarEnteringNewParcel -= scriptevent_OnAvatarEnteringNewParcel;
            m_scene.EventManager.OnSceneGroupMove -= scriptevent_OnSceneGroupMove;
        }
        #endregion

        #region OpenSimulatorEvents


        private void onScriptStart(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }
        private void onScriptRemove(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }

        private void onScriptStop(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }

        private void onScriptReset(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }

        private bool scriptevent_OnSceneGroupMove(UUID groupID, Vector3 delta)
        {
            fireEvent(EventType.EVENT_LINKSETMOVE, new String[] { groupID.ToString(), delta.ToString() });
            return true;
        }

        private void scriptevent_OnAvatarEnteringNewParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            fireEvent(EventType.EVENT_AVATARENTERPARCEL, new String[] { avatar.UUID.ToString() });
        }

        private void scriptevent_OnRemovePresence(UUID agentId)
        {
            fireEvent(EventType.EVENT_REMOVEPRESENCE, new String[] { agentId.ToString() });
        }

        private void scriptevent_OnNewPresence(ScenePresence presence)
        {
            fireEvent(EventType.EVENT_NEWPRESENCE, new String[] { presence.UUID.ToString() });
        }

        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osStartScriptEvents(UUID hostID, UUID scriptID)
        {
            m_listener.Add(scriptID);
            saveRegionListenerToStorage();
        }

        [ScriptInvocation]
        public void osStopScriptEvents(UUID hostID, UUID scriptID)
        {
            m_listener.Remove(scriptID);
            saveRegionListenerToStorage();
        }

        [ScriptInvocation]
        public void osTriggerCustomEvent(UUID hostID, UUID scriptID, int event_type, string event_data)
        {
            fireEvent(EventType.EVENT_CUSTOM, new String[] { event_data });
        }
        #endregion

        #region Functions

        private void fireEvent(EventType type, String[] data)
        {
            List<UUID> removeScripts = new List<UUID>();

            lock (m_listener)
            {
                foreach (UUID itemID in m_listener)
                {
                    try
                    {
                        ScriptEngine.PostScriptEvent(itemID, "region_event", new Object[] { type, data });
                    }
                    catch
                    {
                        m_log.Error("[ScriptEvent] Faild to trigger event '" + type.ToString() + "' for script " + itemID);
                        removeScripts.Add(itemID);
                    }
                }
            }

            cleanupListenerList(removeScripts.ToArray());
        }

        private void cleanupListenerList(UUID[] scripts)
        {
            lock (m_listener)
            {
                foreach (UUID script in scripts)
                    osStopScriptEvents(UUID.Zero, script);
            }
        }

        private void saveRegionListenerToStorage()
        {
            cleanRegionListenerStorage();

            int i = 0;
            foreach (UUID itemID in m_listener)
                m_scene.StoreExtraSetting("regioneventlisten_" + i++, itemID.ToString());
        }

        private void cleanRegionListenerStorage()
        {
            int i = 0;

            while(m_scene.GetExtraSetting("regioneventlisten_" + i) != String.Empty)
                m_scene.RemoveExtraSetting("regioneventlisten_" + i++);
        }
        #endregion
    }
}
