using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.SSRLV
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "SSRLV")]

    class rlv : EmptyModule
    {
        public override string Name
        {
            get { return "SSRLV"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osRLV");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }

            base.World.EventManager.OnNewPresence += onNewPresence;
        }

        #region events
        private void onNewPresence(ScenePresence presence)
        {
            if (!presence.IsChildAgent)
                return;

            if (presence.IsNPC)
                return;

            presence.ControllingClient.OnRezObject += onRezObject;
        }

        private void onRezObject(IClientAPI remoteClient, UUID itemID, UUID GroupID, Vector3 RayEnd, Vector3 RayStart, UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection, bool RezSelected, bool RemoveItem, UUID fromTaskID)
        {
            remoteClient.SendAlertMessage("RLV: You are not allowed to do this!");
            throw new Exception("Operation not allowed!");
        }
        #endregion

    }
}
