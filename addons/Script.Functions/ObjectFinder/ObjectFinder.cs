using Chris.OS.Additions.Utils;
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

    public class ObjectFinder : EmptyModule
    {
        private IScriptModuleComms m_scriptModule;

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
            }else
            {
                base.Logger.Warn("[" + Name + "]: scene == null");
            }
        }
        #endregion

        #region Script Funktions
        [ScriptInvocation]
        public object[] osGetSearchableObjectList(UUID hostID, UUID scriptID, String searchString)
        {
            List<object> returnList = new List<object>();

            foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
            {
                if(thisGroup.Name == searchString)
                    returnList.Add(thisGroup.UUID);
            }

            return returnList.ToArray();
        }

        [ScriptInvocation]
        public object[] osGetSearchableObjectPartList(UUID hostID, UUID scriptID, String searchString)
        {
            List<object> returnList = new List<object>();

            foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
            {
                foreach(SceneObjectPart thisPart in thisGroup.Parts)
                {
                    if (thisPart.Name == searchString)
                        returnList.Add(thisPart.UUID);
                }
            }

            return returnList.ToArray();
        }
        #endregion
    }
}