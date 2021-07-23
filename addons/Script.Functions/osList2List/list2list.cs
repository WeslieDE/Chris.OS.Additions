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

namespace Chris.OS.Additions.Script.Functions.osList2List
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "List2List")]

    class list2list : EmptyModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "List2List"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osList2List");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public object[] osList2List(UUID hostID, UUID scriptID, object[][] datas, int position)
        {
            if(datas.Length <= position)
                return new object[0];

            return datas[position];
        }
        #endregion
    }
}
