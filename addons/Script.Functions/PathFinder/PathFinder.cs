using Chris.OS.Additions.Utils;
using Mono.Addins;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Threading;
using static OpenMetaverse.Primitive;

namespace Chris.OS.Additions.Script.Functions.PathFinder
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "PathFinder")]

    class PathFinder : EmptyModule
    {
        private const String NODE_TEXTURE = "921f6d16-90c8-4926-963b-4698ff107c29";

        private List<NodeInfo> m_nodes = new List<NodeInfo>();
        private Boolean m_scanning = false;

        #region EmptyModule
        public override string Name
        {
            get { return "PathFinder"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();

                m_scriptModule.RegisterScriptInvocation(this, "osStartNodeScan");
                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeListToTarget");
                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeList");
                m_scriptModule.RegisterScriptInvocation(this, "osGetNodeConnections");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }

            base.World.EventManager.OnSceneObjectPartCopy += onSceneObjectPartCopy;
            base.World.EventManager.OnSceneObjectPartUpdated += onSceneObjectPartUpdated;
            base.World.EventManager.OnIncomingSceneObject += onIncomingSceneObject;
            base.World.EventManager.OnSceneObjectLoaded += onSceneObjectLoaded;
            base.World.EventManager.OnNewPresence += onNewPresence;

            collectNodeData();
        }

        #endregion

        #region events
        private void onIncomingSceneObject(SceneObjectGroup so)
        {
            foreach (SceneObjectPart thisPart in so.Parts)
                if (thisPart.Description.ToUpper().Equals("PATH_NODE"))
                {
                    try
                    {
                        collectNodeData();
                    }
                    catch
                    {
                        base.Logger.Error("Error while collectNodeData();");
                    }
                    return;
                }
        }

        private void onSceneObjectPartCopy(SceneObjectPart copy, SceneObjectPart original, bool userExposed)
        {
            if (original.Description.ToUpper().Equals("PATH_NODE"))
            {
                try
                {
                    collectNodeData();
                }
                catch
                {
                    base.Logger.Error("Error while collectNodeData();");
                }
                return;
            }
        }

        private void onSceneObjectPartUpdated(SceneObjectPart sop, bool full)
        {
            if (sop.Description.ToUpper().Equals("PATH_NODE"))
            {
                try
                {
                    collectNodeData();
                }
                catch
                {
                    base.Logger.Error("Error while collectNodeData();");
                }
                return;
            }
        }

        private void onSceneObjectLoaded(SceneObjectGroup so)
        {
            foreach (SceneObjectPart part in so.Parts)
            {
                if (part.Description.ToUpper().Equals("PATH_NODE"))
                {
                    try
                    {
                        collectNodeData();
                    }
                    catch
                    {
                        base.Logger.Error("Error while collectNodeData();");
                    }
                    return;
                }
            }
        }

        private void onNewPresence(ScenePresence presence)
        {
            try
            {
                collectNodeData();
            }
            catch
            {
                base.Logger.Error("Error while collectNodeData();");
            }
        }

        #endregion

        #region funktions
        private void collectNodeData()
        {
            base.Logger.Info("PathFinder: collectNodeData();");

            if (m_scanning == true)
                return;

            m_scanning = true;

            lock (m_nodes)
            {
                try
                {
                    m_nodes.Clear();

                    foreach (SceneObjectGroup thisGroup in base.World.GetSceneObjectGroups())
                    {
                        foreach (SceneObjectPart thisPart in thisGroup.Parts)
                        {
                            if (thisPart.Description.ToUpper().Equals("PATH_NODE"))
                            {
                                base.Logger.Info("PathFinder: Found " + thisPart.UUID.ToString() + " (" + thisPart.Name + ")");

                                NodeInfo info = new NodeInfo();
                                info.ID = thisPart.UUID;
                                info.Name = thisPart.Name;
                                m_nodes.Add(info);
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
                    foreach (NodeInfo node in m_nodes)
                    {
                        SceneObjectPart part = base.World.GetSceneObjectPart(node.ID);

                        if (part == null)
                            continue;

                        if (part.Inventory == null)
                            continue;

                        foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                        {
                            if(item.Type == 7)
                            {
                                NodeInfo ni = m_nodes.Find(x => x.Name.Equals(item.Name));

                                if (!node.Connections.Contains(ni.ID))
                                    node.Connections.Add(ni.ID);

                                if (ni != null)
                                    if (!ni.Connections.Contains(part.UUID))
                                        ni.Connections.Add(part.UUID);
                            }
                        }
                    }

                }
                catch (Exception error)
                {
                    base.Logger.Error("Fatal Error while collectNodeData() CONNECT: " + error.Message);
                    base.Logger.Error(error.StackTrace);
                }
            }

            m_scanning = false;
        }

        private List<NodeInfo> getCopyOfNodeList()
        {
            List<NodeInfo> returnData = new List<NodeInfo>();

            foreach(NodeInfo thisNodeInfo in m_nodes)
            {
                NodeInfo newNodeInfo = new NodeInfo();
                newNodeInfo.ID = thisNodeInfo.ID;
                newNodeInfo.Name = thisNodeInfo.Name;
                newNodeInfo.Connections = thisNodeInfo.Connections;

                returnData.Add(newNodeInfo);
            }

            return returnData;
        }

        #endregion

        #region Script function

        [ScriptInvocation]
        public void osStartNodeScan(UUID hostID, UUID scriptID)
        {
            collectNodeData();
        }

        [ScriptInvocation]
        public object[] osGetNodeList(UUID hostID, UUID scriptID)
        {
            List<object> returnData = new List<object>();

            foreach (NodeInfo node in m_nodes)
                returnData.Add(node.ID);

            return returnData.ToArray();
        }

        [ScriptInvocation]
        public object[] osGetNodeConnections(UUID hostID, UUID scriptID, UUID nodeID)
        {
            List<object> returnData = new List<object>();

            NodeInfo nodeInfo = m_nodes.Find(x => x.ID.Equals(nodeID));

            if (nodeInfo == null)
                throw new Exception("Cant find node");

            foreach (UUID id in nodeInfo.Connections)
                returnData.Add(id);

            return returnData.ToArray();
        }
        

        [ScriptInvocation]
        public object[] osGetNodeListToTarget(UUID hostID, UUID scriptID, UUID start, UUID end)
        {
            List<NodeInfo> nodes = getCopyOfNodeList();
            List<NodeInfo> workspace = new List<NodeInfo>();
            List<object> outputList = new List<object>();

            NodeInfo startNodeInfo = nodes.Find(x => x.ID.Equals(start));
            NodeInfo endNodeInfo = nodes.Find(x => x.ID.Equals(end));

            if (startNodeInfo == null)
                throw new Exception("Cant find start node");

            if (startNodeInfo == null)
                throw new Exception("Cant find end node");

            if (startNodeInfo.Connections.Count == 0)
                throw new Exception("Start node have no conecctions");

            if (endNodeInfo.Connections.Count == 0)
                throw new Exception("End node have no conecctions");

            startNodeInfo.ParentNode = startNodeInfo.ID;
            workspace.Add(startNodeInfo);

            NodeInfo currentNode = startNodeInfo;

            try
            {
                while (currentNode != null)
                {
                    currentNode.ParentNode = currentNode.ID;

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
                            else
                            {
                                base.Logger.Info("osGetNodeListToTarget(): parentNode allready set with " + ni.ParentNode.ToString());
                            }
                        }
                        else
                        {
                            base.Logger.Info("osGetNodeListToTarget(): Cant find node for id " + thisNodeID.ToString());
                        }
                    }

                    currentNode = workspace.Find(x => x.ParentNode == UUID.Zero);
                }
            }catch(Exception error)
            {
                base.Logger.Error("Error while get path line: " + error.Message);
                base.Logger.Error(error.StackTrace);
            }

            foreach(NodeInfo node in workspace)
                outputList.Add(JsonConvert.SerializeObject(node));

            //outputList.Reverse();
            return outputList.ToArray();
        }
        #endregion
    }
}
