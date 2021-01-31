using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;

[assembly: Addin("GetInventoryList", "0.1")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]
namespace OpenSim.TimerThread
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "GetInventoryList")]

    class GetInventoryList : INonSharedRegionModule
    {
        private Dictionary<UUID, Timer> m_timers = new Dictionary<UUID, Timer>();

        private Scene m_scene = null;

        #region INonSharedRegionModule
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get { return "GetInventoryList"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {

        }

        public void Close()
        {

        }

        public void Initialise(IConfigSource source)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            m_scene = scene;

            try
            {
                IScriptModuleComms m_scriptModule = m_scene.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osGetInventoryList");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            foreach (Timer timer in m_timers.Values)
            {
                timer.Stop();
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public object[] osGetInventoryList(UUID hostID, UUID scriptID)
        {
            List<object> returnList = new List<object>();

            SceneObjectPart part = m_scene.GetSceneObjectPart(hostID);
            
            foreach(TaskInventoryItem item in part.Inventory.GetInventoryItems())
                returnList.Add(item.Name);

            return returnList.ToArray();
        }
        #endregion
    }
}
