using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;

namespace Chris.OS.Additions.Script.Functions.OwnerSay
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OwnerSay")]

    class OwnerSay : EmptyModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "OwnerSay"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osOwnerSay");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osOwnerSay(UUID hostID, UUID scriptID, UUID user, String message)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            World.SimChatBroadcast(OpenMetaverse.Utils.StringToBytes(message), ChatTypeEnum.Owner, 0,
                       part.AbsolutePosition, part.Name, hostID, false);
        }
        #endregion
    }
}
