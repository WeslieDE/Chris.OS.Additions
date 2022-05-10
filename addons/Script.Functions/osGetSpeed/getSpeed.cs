using Chris.OS.Additions.Utils;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;

namespace Chris.OS.Additions.Script.Functions.osGetSpeed
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OpenSimLand")]

    internal class getSpeed : EmptyNonSharedModule
    {

        #region EmptyModule
        public override string Name
        {
            get { return "getSpeedScriptCommand"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osGetSpeed");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public float osGetSpeed(UUID hostID, UUID scriptID, String target)
        {
            try
            {
                UUID avatarID = UUID.Parse(target);

                ScenePresence avatar = base.World.GetScenePresence(avatarID);

                if (avatar != null)
                    return avatar.SpeedModifier;
            }
            catch
            {
                return 0;
            }

            return 0;
        }
        #endregion
    }
}