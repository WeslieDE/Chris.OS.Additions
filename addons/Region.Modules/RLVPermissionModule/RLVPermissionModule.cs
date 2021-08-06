using Chris.OS.Additions.Utils;
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

namespace Chris.OS.Additions.Region.Modules.RLVPermissionModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RLVPermissionModule")]

    class RLVPermissionModule : EmptyModule
    {
        private Dictionary<UUID, UserData> m_userdata = new Dictionary<UUID, UserData>();

        #region EmptyModule
        public override string Name
        {
            get { return "RLVPermissionModule"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.Permissions.OnRezObject += onRezObject;
            base.World.Permissions.OnEditObject += onEditObject;
            base.World.Permissions.OnTeleport += onTeleport;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osRLV");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }

        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osRLV(UUID hostID, UUID scriptID, UUID user, String rule, String value)
        {
            setDataValue(user, rule, value);
        }
        #endregion

        #region Events
        private bool onRezObject(int objectCount, UUID owner, Vector3 objectPosition)
        {
            if (getDataValue(owner, "rez") == "n")
            {
                sendMessage(owner, "RLV: You are not allowed to do this.");
                return false;
            }

            return true;
        }


        private bool onEditObject(SceneObjectGroup sog, ScenePresence sp)
        {
            if (getDataValue(sp.UUID, "edit") == "n")
            {
                sendMessage(sp.UUID, "RLV: You are not allowed to do this.");
                return false;
            }

            return true;
        }

        private bool onTeleport(UUID userID, Scene scene)
        {
            if (getDataValue(userID, "teleport") == "n")
            {
                sendMessage(userID, "RLV: You are not allowed to do this.");
                return false;
            }

            return true;
        }

        #endregion

        #region Helpers
        private void sendMessage(UUID target, String message)
        {
            ScenePresence p = base.World.GetScenePresence(target);

            if (p == null)
                return;

            if (p.IsNPC)
                return;

            p.ControllingClient.SendAlertMessage(message);
        }

        private String getDataValue(UUID user, String key)
        {
            UserData ud = null;

            if(m_userdata.TryGetValue(user, out ud))
            {
                String value = null;

                if(ud.Rules.TryGetValue(key, out value))
                    return value;

                return null;
            }

            return null;
        }

        private void setDataValue(UUID user, String key, String value)
        {
            lock(m_userdata)
            {
                UserData ud = null;

                if (m_userdata.TryGetValue(user, out ud))
                {
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
