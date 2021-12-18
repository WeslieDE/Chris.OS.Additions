using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.RemoteScriptPin
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OwnerSay")]

    class RemoteScriptPin : EmptyNonSharedModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "RemoteScriptPin"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osRemoteScriptPin");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osRemoteScriptPin(UUID hostID, UUID scriptID, UUID prim, int pin)
        {
            SceneObjectPart host = base.World.GetSceneObjectPart(hostID);
            SceneObjectPart part = base.World.GetSceneObjectPart(prim);

            if (part == null)
                throw new Exception("Object not found!");

            if(host.OwnerID != part.OwnerID)
                throw new Exception("Owner dont match!");

            part.ScriptAccessPin = pin;
        }
        #endregion

    }
}
