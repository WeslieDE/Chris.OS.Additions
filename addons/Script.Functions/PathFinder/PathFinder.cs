using Chris.OS.Additions.Utils;
using Mono.Addins;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Script.Functions.PathFinder
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "PathFinder")]
    class PathFinder : EmptyModule
    {
        private Dictionary<String, object[]> m_cache = new Dictionary<String, object[]>();

        #region EmptyModule
        public override string Name
        {
            get { return "PathFinder"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.Permissions.OnRezObject += onRezObject;
            base.World.Permissions.OnDeleteObject += onDeleteObject;
            base.World.Permissions.OnDuplicateObject += onDuplicateObject;
            base.World.Permissions.OnEditObject += onEditObject;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeListToTarget");
                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeList");
                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeConnections");
                m_scriptModule.RegisterScriptInvocation(this, "osClearNodeCache");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }

        #endregion

        #region Events
        private bool onEditObject(SceneObjectGroup sog, ScenePresence sp)
        {
            lock (m_cache)
            {
                m_cache.Clear();
                return true;
            }
        }

        private bool onDuplicateObject(SceneObjectGroup sog, ScenePresence sp)
        {
            lock (m_cache)
            {
                m_cache.Clear();
                return true;
            }
        }

        private bool onDeleteObject(SceneObjectGroup sog, ScenePresence sp)
        {
            lock (m_cache)
            {
                m_cache.Clear();
                return true;
            }
        }

        private bool onRezObject(int objectCount, UUID owner, Vector3 objectPosition)
        {
            lock(m_cache)
            {
                m_cache.Clear();
                return true;
            }
        }
        #endregion

        #region funktions
        private List<NodeInfo> collectNodeData()
        {
            List<NodeInfo> nodes = new List<NodeInfo>();

            try
            {
                foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
                {
                    foreach (SceneObjectPart thisPart in thisGroup.Parts)
                    {
                        if (thisPart.Description.ToUpper().Equals("PATH_NODE"))
                        {
                            NodeInfo info = new NodeInfo();
                            info.ID = thisPart.UUID;
                            info.Name = thisPart.Name;
                            nodes.Add(info);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                base.Logger.Error("Fatal Error while collectNodeData() SCAN: " + error.Message);
                base.Logger.Error(error.StackTrace);
            }

            try
            {
                foreach (NodeInfo node in nodes)
                {
                    SceneObjectPart part = base.World.GetSceneObjectPart(node.ID);

                    if (part == null)
                        continue;

                    if (part.Inventory == null)
                        continue;

                    foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                    {
                        if (item.Type == 7)
                        {
                            NodeInfo ni = nodes.Find(x => x.Name.Equals(item.Name));

                            if(ni != null)
                            {
                                if (!node.Connections.Contains(ni.ID))
                                    node.Connections.Add(ni.ID);

                                if (!ni.Connections.Contains(part.UUID))
                                    ni.Connections.Add(part.UUID);
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                base.Logger.Error("Fatal Error while collectNodeData() CONNECT: " + error.Message);
                base.Logger.Error(error.StackTrace);
            }

            return nodes;
        }
        #endregion

        #region Script function


        [ScriptInvocation]
        public void osClearNodeCache(UUID hostID, UUID scriptID)
        {
            lock (m_cache)
            {
                m_cache.Clear();
            }
        }

        [ScriptInvocation]
        public object[] osGetNodeList(UUID hostID, UUID scriptID)
        {
            List<object> returnData = new List<object>();

            foreach (NodeInfo node in collectNodeData())
                returnData.Add(node.ID);

            return returnData.ToArray();
        }

        [ScriptInvocation]
        public object[] osGetNodeConnections(UUID hostID, UUID scriptID, UUID nodeID)
        {
            List<object> returnData = new List<object>();

            NodeInfo nodeInfo = collectNodeData().Find(x => x.ID.Equals(nodeID));

            if (nodeInfo == null)
                return returnData.ToArray();

            foreach (UUID id in nodeInfo.Connections)
                returnData.Add(id);

            return returnData.ToArray();
        }

        [ScriptInvocation]
        public object[] osGetNodeListToTarget(UUID hostID, UUID scriptID, UUID start, UUID end)
        {
            if (m_cache.TryGetValue(start.ToString() + ":" + end.ToString(), out object[] cache))
                return cache;

            List<NodeInfo> nodes = collectNodeData();
            List<NodeInfo> workspace = new List<NodeInfo>();
            List<object> outputList = new List<object>();

            NodeInfo startNodeInfo = nodes.Find(x => x.ID.Equals(start));
            NodeInfo endNodeInfo = nodes.Find(x => x.ID.Equals(end));

            if (startNodeInfo == null)
                throw new ScriptException("Cant find start node");

            if (startNodeInfo == null)
                throw new ScriptException("Cant find end node");

            if (startNodeInfo.Connections.Count == 0)
                throw new ScriptException("Start node have no conecctions");

            if (endNodeInfo.Connections.Count == 0)
                throw new ScriptException("End node have no conecctions");

            startNodeInfo.ParentNode = startNodeInfo.ID;
            workspace.Add(startNodeInfo);

            NodeInfo currentNode = startNodeInfo;

            try
            {
                while (currentNode != null)
                {
                    currentNode.Checked = true;

                    foreach (UUID thisNodeID in currentNode.Connections)
                    {
                        NodeInfo ni = nodes.Find(x => x.ID == thisNodeID);

                        if (ni != null)
                        {
                            if(ni.ParentNode == UUID.Zero)
                            {
                                ni.ParentNode = currentNode.ID;
                                workspace.Add(ni);
                            }
                        }
                    }

                    currentNode = workspace.Find(x => x.Checked == false);
                }
            }catch(Exception error)
            {
                base.Logger.Error("Error while get path line: " + error.Message);
                base.Logger.Error(error.StackTrace);
            }

            if (endNodeInfo.ParentNode == UUID.Zero)
                throw new ScriptException("cant find path");

            currentNode = endNodeInfo;
            while(currentNode.ID != startNodeInfo.ID)
            {
                NodeInfo ni = nodes.Find(x => x.ID == currentNode.ParentNode);

                outputList.Add(currentNode.ParentNode);
                currentNode = ni;
            }

            outputList.Reverse();
            outputList.Add(endNodeInfo.ID);

            m_cache.Add(start.ToString() + ":" + end.ToString(), outputList.ToArray());

            return outputList.ToArray();
        }
        #endregion
    }
}
