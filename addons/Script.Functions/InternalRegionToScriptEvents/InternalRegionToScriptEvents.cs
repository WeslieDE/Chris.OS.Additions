using Chris.OS.Additions.Script.Functions.DataValue;
using Chris.OS.Additions.Script.Functions.EasyDialog;
using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;


namespace Chris.OS.Additions.Script.Functions.InternalRegionToScriptEvents
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "InternalRegionToScriptEvents")]

    public class InternalRegionToScriptEvents : EmptyModule
    {
        private List<UUID> m_listener = new List<UUID>();
        private IScriptModule ScriptEngine = null;

        #region EmptyModule

        public override string Name
        {
            get { return "InternalRegionToScriptEvents"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osStartScriptEvents");
                m_scriptModule.RegisterScriptInvocation(this, "osStopScriptEvents");
                m_scriptModule.RegisterScriptInvocation(this, "osTriggerCustomEvent");

                m_scriptModule.RegisterConstant("EVENT_CUSTOM", 0);
                m_scriptModule.RegisterConstant("EVENT_NEWPRESENCE", 1);
                m_scriptModule.RegisterConstant("EVENT_REMOVEPRESENCE", 2);
                m_scriptModule.RegisterConstant("EVENT_AVATARENTERPARCEL", 3);

                m_scriptModule.RegisterConstant("EVENT_DATASTORAGESET", 1001);
                m_scriptModule.RegisterConstant("EVENT_DATASTORAGEREMOVE", 1002);

                m_scriptModule.RegisterConstant("EVENT_EASYDIALOGTIMEOUT", 2001);

                m_scriptModule.RegisterConstant("EVENT_GENERIC", 42001337);
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: Script method registration failed; {0}", e.Message);
            }

            ScriptEngine = base.World.RequestModuleInterface<IScriptModule>();

            base.World.EventManager.OnScriptReset += onScriptReset;
            base.World.EventManager.OnRemoveScript += onScriptRemove;

            //DataStorage Events
            DataStorageEvents.onDeleteDataValue += scriptevent_onDeleteDataValue;
            DataStorageEvents.onSetDataValue += scriptevent_onSetDataValue;
            DataStorageEvents.onRateLimit += scriptevent_onRateLimit;

            //EasyDialog Events
            EasyDialogEvents.onDialogTimeout += scriptevent_onDialogTimeout;

            //Events for Scripts
            base.World.EventManager.OnNewPresence += scriptevent_OnNewPresence;
            base.World.EventManager.OnRemovePresence += scriptevent_OnRemovePresence;
            base.World.EventManager.OnAvatarEnteringNewParcel += scriptevent_OnAvatarEnteringNewParcel;
        }

        public override void RemoveRegion(Scene scene)
        {
            base.World.EventManager.OnScriptReset -= onScriptReset;
            base.World.EventManager.OnRemoveScript -= onScriptRemove;

            //DataStorage Events
            DataStorageEvents.onDeleteDataValue -= scriptevent_onDeleteDataValue;
            DataStorageEvents.onSetDataValue -= scriptevent_onSetDataValue;
            DataStorageEvents.onRateLimit -= scriptevent_onRateLimit;

            //EasyDialog Events
            EasyDialogEvents.onDialogTimeout -= scriptevent_onDialogTimeout;

            //Events for Scripts
            base.World.EventManager.OnNewPresence -= scriptevent_OnNewPresence;
            base.World.EventManager.OnRemovePresence -= scriptevent_OnRemovePresence;
            base.World.EventManager.OnAvatarEnteringNewParcel -= scriptevent_OnAvatarEnteringNewParcel;
        }
        #endregion

        #region OpenSimulatorEvents

        private void onScriptRemove(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }

        private void onScriptReset(uint localID, UUID itemID)
        {
            osStopScriptEvents(UUID.Zero, itemID);
        }

        private void scriptevent_OnAvatarEnteringNewParcel(ScenePresence avatar, int localLandID, UUID regionID)
        {
            fireEvent(ScriptEventTypes.EVENT_AVATARENTERPARCEL, avatar.UUID.ToString());
        }

        private void scriptevent_OnRemovePresence(UUID agentId)
        {
            fireEvent(ScriptEventTypes.EVENT_REMOVEPRESENCE, agentId.ToString());
        }

        private void scriptevent_OnNewPresence(ScenePresence presence)
        {
            fireEvent(ScriptEventTypes.EVENT_NEWPRESENCE, presence.UUID.ToString());
        }

        //Data Storage Events
        private void scriptevent_onDeleteDataValue(string key)
        {
            fireEvent(ScriptEventTypes.EVENT_DATASTORAGEREMOVE, key);
        }

        private void scriptevent_onSetDataValue(string key, string data)
        {
            fireEvent(ScriptEventTypes.EVENT_DATASTORAGESET, key);
        }

        private void scriptevent_onRateLimit()
        {
            fireEvent(ScriptEventTypes.EVENT_DATASTORAGERATELIMIT, "");
        }

        private void scriptevent_onDialogTimeout(int listenerID, UUID userID)
        {
            fireEvent(ScriptEventTypes.EVENT_EASYDIALOGTIMEOUT, listenerID + "," + userID.ToString());
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
        public void osTriggerCustomEvent(UUID hostID, UUID scriptID, string event_data)
        {
            fireEvent(ScriptEventTypes.EVENT_CUSTOM, event_data);
        }
        #endregion

        #region Functions
        private void fireEvent(ScriptEventTypes type, string data)
        {
            List<UUID> removeScripts = new List<UUID>();

            lock (m_listener)
            {
                foreach (UUID itemID in m_listener)
                {
                    try
                    {
                        ScriptEngine.PostScriptEvent(itemID, "link_message", new Object[] { type.GetHashCode(), ScriptEventTypes.EVENT_GENERIC.GetHashCode(), data, UUID.Zero.ToString() });
                    }
                    catch
                    {
                        base.Logger.Error("[ScriptEvent] Faild to trigger event '" + type.ToString() + "' for script " + itemID);
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
                base.World.StoreExtraSetting("regioneventlisten_" + i++, itemID.ToString());
        }

        private void cleanRegionListenerStorage()
        {
            int i = 0;

            while(base.World.GetExtraSetting("regioneventlisten_" + i) != String.Empty)
                base.World.RemoveExtraSetting("regioneventlisten_" + i++);
        }
        #endregion
    }
}