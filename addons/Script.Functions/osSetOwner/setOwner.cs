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


namespace Chris.OS.Additions.Script.Functions.osSetOwner
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "osSetOwner")]
    public class setOwner : EmptyNonSharedModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "osSetOwner"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osSetOwner");
                m_scriptModule.RegisterScriptInvocation(this, "osSetGroup");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osSetOwner(UUID hostID, UUID scriptID, UUID prim, UUID newOwner)
        {
            SceneObjectPart host = base.World.GetSceneObjectPart(hostID);
            SceneObjectPart part = base.World.GetSceneObjectPart(prim);

            if (host == null)
                throw new Exception("Host object not found!");

            if (part == null)
                throw new Exception("Part object not found!");

            ScenePresence avatar = base.World.GetScenePresence(host.OwnerID);

            if (avatar == null)
                throw new Exception("Owner not found!");

            if (!avatar.IsGod)
                throw new Exception("osSetOwner can only be executed by God!");

            part.OwnerID = newOwner;
        }
         
        [ScriptInvocation]
        public void osSetGroup(UUID hostID, UUID scriptID, UUID prim, UUID newGroup)
        {
            SceneObjectPart host = base.World.GetSceneObjectPart(hostID);
            SceneObjectPart part = base.World.GetSceneObjectPart(prim);

            if (host == null)
                throw new Exception("Host object not found!");

            if (part == null)
                throw new Exception("Part object not found!");

            ScenePresence avatar = base.World.GetScenePresence(host.OwnerID);

            if (avatar == null)
                throw new Exception("Owner not found!");

            if (!avatar.IsGod)
                throw new Exception("osSetGroup can only be executed by God!");

            part.GroupID = newGroup;
        }
        #endregion
    }
}
