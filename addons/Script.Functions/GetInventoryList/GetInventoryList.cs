using Chris.OS.Additions.Utils;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.GetInventoryList
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "GetInventoryList")]

    public class GetInventoryList : EmptyModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "GetInventoryList"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osGetInventoryList");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public object[] osGetInventoryList(UUID hostID, UUID scriptID, String target = "00000000-0000-0000-0000-000000000000")
        {
            UUID targetHost = hostID;

            if(!target.Equals(UUID.Zero.ToString()))
                if (!UUID.TryParse(target, out targetHost))
                    throw new Exception("target is not a valid uuid");

            List<object> returnList = new List<object>();

            SceneObjectPart part = base.World.GetSceneObjectPart(targetHost);

            if(part == null)
                throw new Exception("target not found");

            foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                returnList.Add(item.Name);

            return returnList.ToArray();
        }
        #endregion
    }
}