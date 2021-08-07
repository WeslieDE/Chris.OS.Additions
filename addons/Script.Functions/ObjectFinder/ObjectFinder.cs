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
using System.Collections.ObjectModel;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.ObjectFinder
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ObjectFinder")]

    public class ObjectFinder : EmptyModule
    {
        private IScriptModuleComms m_scriptModule;
        private List<CacheData> m_GroupCache = new List<CacheData>();
        private List<CacheData> m_PartCache = new List<CacheData>();

        #region EmptyModule
        public override string Name
        {
            get { return "ObjectFinder"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            if(base.World != null)
            {
                try
                {
                    m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();

                    m_scriptModule.RegisterScriptInvocation(this, "osGetSearchableObjectList");
                    m_scriptModule.RegisterScriptInvocation(this, "osGetSearchableObjectPartList");
                }
                catch (Exception e)
                {
                    base.Logger.WarnFormat("[" + Name + "]: Script method registration failed; {0}", e.Message);
                }

                base.World.EventManager.OnSceneObjectLoaded += onSceneObjectLoaded;
                base.World.EventManager.OnSceneObjectPartCopy += onSceneObjectPartCopy;
                base.World.EventManager.OnSceneObjectPartUpdated += onSceneObjectPartUpdated;
                base.World.EventManager.OnIncomingSceneObject += onIncomingSceneObject;
            }
            else
            {
                base.Logger.Warn("[" + Name + "]: scene == null");
            }
        }
        #endregion

        #region Events

        private void onSceneObjectPartCopy(SceneObjectPart copy, SceneObjectPart original, bool userExposed)
        {
            m_GroupCache.Clear();
            m_PartCache.Clear();
        }

        private void onSceneObjectLoaded(SceneObjectGroup so)
        {
            m_GroupCache.Clear();
            m_PartCache.Clear();
        }

        private void onSceneObjectPartUpdated(SceneObjectPart sop, bool full)
        {
            m_GroupCache.Clear();
            m_PartCache.Clear();
        }

        private void onIncomingSceneObject(SceneObjectGroup so)
        {
            m_GroupCache.Clear();
            m_PartCache.Clear();
        }

        #endregion

        #region Script Funktions
        [ScriptInvocation]
        public object[] osGetSearchableObjectList(UUID hostID, UUID scriptID, String searchString)
        {
            SceneObjectPart host = base.World.GetSceneObjectPart(hostID);
            CacheData cache = m_GroupCache.Find(x => x.searchString.Equals(searchString));

            if (cache != null)
                return cache.results.ToArray();

            List<ObjectData> dataList = new List<ObjectData>();
            List<object> returnList = new List<object>();

            foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
            {
                float dis = Vector3.Distance(thisGroup.AbsolutePosition, host.AbsolutePosition);

                if(thisGroup.Name == searchString)
                    dataList.Add(new ObjectData(thisGroup.Name, thisGroup.UUID, dis));
            }

            dataList.Sort((x, y) => x.Distance.CompareTo(y.Distance));

            foreach (ObjectData obj in dataList)
                returnList.Add(obj.ID);

            m_GroupCache.Add(new CacheData(searchString, returnList));
            return returnList.ToArray();
        }

        [ScriptInvocation]
        public object[] osGetSearchableObjectPartList(UUID hostID, UUID scriptID, String searchString)
        {
            SceneObjectPart host = base.World.GetSceneObjectPart(hostID);
            CacheData cache = m_PartCache.Find(x => x.searchString.Equals(searchString));

            if (cache != null)
                return cache.results.ToArray();

            List<ObjectData> dataList = new List<ObjectData>();
            List<object> returnList = new List<object>();

            foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
            {
                foreach(SceneObjectPart thisPart in thisGroup.Parts)
                {
                    float dis = Vector3.Distance(thisPart.AbsolutePosition, host.AbsolutePosition);

                    if (thisPart.Name == searchString)
                        dataList.Add(new ObjectData(thisPart.Name, thisPart.UUID, dis));
                }
            }

            dataList.Sort((x, y) => x.Distance.CompareTo(y.Distance));

            foreach (ObjectData obj in dataList)
                returnList.Add(obj.ID);

            m_PartCache.Add(new CacheData(searchString, returnList));
            return returnList.ToArray();
        }
        #endregion
    }
}