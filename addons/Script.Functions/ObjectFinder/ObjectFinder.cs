using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.ObjectFinder
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ObjectFinder")]

    public class ObjectFinder : INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        private IScriptModuleComms m_scriptModule;

        #region INonSharedRegionModule
        public string Name
        {
            get { return "ObjectFinder"; }
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
                m_scriptModule = m_scene.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osGetSearchableObjectList");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }

        public void RemoveRegion(Scene scene)
        {

        }

        #endregion

        #region Script Funktions
        [ScriptInvocation]
        public object[] osGetSearchableObjectList(UUID hostID, UUID scriptID, String searchString)
        {
            List<object> returnList = new List<object>();

            foreach (SceneObjectGroup thisGroup in m_scene.GetSceneObjectGroups())
            {
                if(thisGroup.Name == searchString)
                    returnList.Add(thisGroup.UUID);
            }

            return returnList.ToArray();
        }
        #endregion
    }
}
