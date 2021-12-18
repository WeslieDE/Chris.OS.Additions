using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Region.Modules.RunntimePermissionModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RunntimePermissionModule")]

    class RunntimePermissionModule : EmptyNonSharedModule
    {
        private Dictionary<UUID, UserData> m_userdata = new Dictionary<UUID, UserData>();

        #region EmptyModule
        public override string Name
        {
            get { return "RunntimePermissionModule"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.Permissions.OnRezObject += onRezObject;
            base.World.Permissions.OnDeleteObject += onDeleteObject;
            
            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osSetExtraPermission");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }

        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osSetExtraPermission(UUID hostID, UUID scriptID, UUID user, String rule, String value)
        {
            if(value.Trim().Equals(String.Empty))
            {
                removeDataValue(user, rule.ToLower().Trim());
            }
            else
            {
                setDataValue(user, rule.ToLower().Trim(), value);
            }
        }
        #endregion

        #region Events
        private bool onRezObject(int objectCount, UUID owner, Vector3 objectPosition)
        {
            if (getDataValue(owner, "rez") == "n")
                return false;

            return true;
        }


        private bool onDeleteObject(SceneObjectGroup sog, ScenePresence sp)
        {
            if (getDataValue(sp.UUID, "delete") == "n")
                return false;

            return true;
        }
        #endregion

        #region Helpers
        private String getDataValue(UUID user, String key)
        {
            UserData ud = null;

            if(m_userdata.TryGetValue(user, out ud))
            {
                if(ud.Rules.TryGetValue(key, out String value))
                    return value;

                return "";
            }

            return "";
        }

        private void setDataValue(UUID user, String key, String value)
        {
            lock(m_userdata)
            {
                UserData ud = null;

                if (m_userdata.TryGetValue(user, out ud))
                {
                    if (ud.Rules.ContainsKey(key))
                        ud.Rules.Remove(key);

                    ud.Rules.Add(key, value);
                }
                else
                {
                    ud = new UserData();
                    ud.Rules.Add(key, value);
                    m_userdata.Add(user, ud);
                }
            }
        }

        private void removeDataValue(UUID user, String key)
        {
            lock (m_userdata)
            {
                UserData ud = null;

                if (m_userdata.TryGetValue(user, out ud))
                    ud.Rules.Remove(key);
            }
        }
        #endregion
    }
}
