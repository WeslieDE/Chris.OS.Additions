using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.CoreModules.World.LegacyMap;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api;
using Roy_T.AStar.Graphs;
using Roy_T.AStar.Grids;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading;

using LSL_Float = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat;
using LSL_Integer = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger;
using LSL_Key = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_List = OpenSim.Region.ScriptEngine.Shared.LSL_Types.list;
using LSL_Rotation = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion;
using LSL_String = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_Vector = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3;

[assembly: Addin("BasicPathFindingModule", "0.1")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]
namespace OpenSim.Modules.PathFinding
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "BasicPathFindingModule")]

    public class BasicPathFindingModule : INonSharedRegionModule
    {
        #region Region Module
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        private IConfig m_config = null;
        private bool m_enabled = true;
        private IScriptModuleComms m_scriptModule;

        private List<Environment> m_environments = new List<Environment>();

        public string Name
        {
            get { return "BasicPathFindingModule"; }
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
            try
            {
                m_config = source.Configs["XEngine"];

                if (m_config != null)
                {
                    m_enabled = m_config.GetBoolean("EnablePathFinding", m_enabled);
                }
                else
                {
                    m_log.Error("[" + Name + "]: Cant find config.");
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[" + Name + "]: initialization error: {0}", e.Message);
                return;
            }

            if (m_enabled)
            {
                m_log.Info("[" + Name + "]: module is enabled");
            }
            else
            {
                m_log.Info("[" + Name + "]: module is disabled");
            }
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_enabled)
            {
                m_log.Info("[" + Name + "]: Load region " + scene.Name);

                m_scene = scene;
                m_scriptModule = m_scene.RequestModuleInterface<IScriptModuleComms>();
                if (m_scriptModule == null)
                {
                    m_log.ErrorFormat("[" + Name + "]: Failed to load IScriptModuleComms!");
                    m_enabled = false;
                    return;
                }

                try
                {
                    m_scriptModule.RegisterScriptInvocation(this, "osGeneratePathEnv");
                    m_scriptModule.RegisterScriptInvocation(this, "osKeepAlivePathEnv");
                    m_scriptModule.RegisterScriptInvocation(this, "osSetPathPositionData");
                    m_scriptModule.RegisterScriptInvocation(this, "osSetPathLineData");
                    m_scriptModule.RegisterScriptInvocation(this, "osGeneratePath");

                    m_scriptModule.RegisterScriptInvocation(this, "osGetSearchableObjectList");

                    m_scriptModule.RegisterScriptInvocation(this, "osGenerateDebugImage");

                    m_scriptModule.RegisterConstant("PATH_ENV_SUCCESSFUL", 19850);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_NOT_FOUND", 19851);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_OUT_OF_RANGE", 19852);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_NOT_IN_LINE", 19853);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_START_OR_END_UNKNOWN", 19854);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_TARGET_NOT_REACHABLE", 19855);
                    m_scriptModule.RegisterConstant("PATH_ENV_ERR_UNKNOWN", 19860);
                }
                catch (Exception e)
                {
                    m_log.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
                    m_enabled = false;
                }

                m_log.Info("[" + Name + "]: Region loading done!");
            }
        }

        public void RemoveRegion(Scene scene)
        {

        }

        #endregion

        #region Asyn Funktions

        private void generatePathEnvironment(ScriptRequestData requestData)
        {
            lock(m_environments)
            {
                try
                {
                    UUID _envID = UUID.Random();
                    Environment _newEnv = new Environment(_envID.ToString(), (int)m_scene.RegionInfo.RegionSizeX);

                    m_environments.Add(_newEnv);

                    m_scriptModule.DispatchReply(requestData.ScriptID, 19850, _envID.ToString(), requestData.RequestID.ToString());
                }
                catch (Exception _error)
                {
                    m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
                }
            }
        }

        private void keepAlivePathEnv(ScriptRequestData requestData)
        {
            lock (m_environments)
            {
                try
                {
                    Environment _env = m_environments.Find(X => X.ID == requestData.EnvironmentID);

                    if (_env != null)
                    {
                        _env.LastTimeUsed = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        m_scriptModule.DispatchReply(requestData.ScriptID, 19850, "", requestData.RequestID.ToString());
                        return;
                    }

                    m_scriptModule.DispatchReply(requestData.ScriptID, 19851, "", requestData.RequestID.ToString());
                }
                catch (Exception _error)
                {
                    m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
                }
            }
        }

        private void setPositionData(ScriptRequestData requestData, Vector3 position, int walkable, int isTarget, int isStart)
        {
            lock(m_environments)
            {
                try
                {
                    Environment _env = m_environments.Find(X => X.ID == requestData.EnvironmentID);

                    if (_env != null)
                    {
                        PathNode _node = _env.Nodes.Find(X => X.PositionX == (int)position.X && X.PositionY == (int)position.Y);

                        if (_node == null)
                        {
                            _node = new PathNode((int)position.X, (int)position.Y, false);
                            _env.Nodes.Add(_node);
                        }

                        if (walkable == 1)
                            _node.Walkable = true;

                        if (walkable == 0)
                            _node.Walkable = false;

                        if (isTarget == 1)
                            _env.Target = _node;

                        if (isStart == 1)
                            _env.Start = _node;

                        return;
                    }

                    m_scriptModule.DispatchReply(requestData.ScriptID, 19851, "", requestData.RequestID.ToString());
                }
                catch (Exception _error)
                {
                    m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
                }
            }
        }

        private void setLineData(ScriptRequestData requestData, Vector3 start, Vector3 target, int walkable)
        {
            lock(m_environments)
            {
                try
                {
                    Environment _env = m_environments.Find(X => X.ID == requestData.EnvironmentID);

                    if (_env != null)
                    {
                        if ((int)start.X == (int)target.X || (int)start.Y == (int)target.Y)
                        {
                            if ((int)start.X == (int)target.X && (int)start.Y == (int)target.Y)
                            {
                                m_scriptModule.DispatchReply(requestData.ScriptID, 19850, "", requestData.RequestID.ToString());
                                return;
                            }

                            Vector3 _PointA = new Vector3(0, 0, 0);
                            Vector3 _PointB = new Vector3(0, 0, 0);

                            if ((int)start.X != (int)target.X)
                            {
                                if ((int)start.X < (int)target.X)
                                {
                                    _PointA = start;
                                    _PointB = target;
                                }

                                if ((int)start.X > (int)target.X)
                                {
                                    _PointA = target;
                                    _PointB = start;
                                }

                                while ((int)_PointA.X <= (int)_PointB.X)
                                {
                                    setPositionData(requestData, _PointA, walkable, 0, 0);
                                    _PointA.X = (int)_PointA.X + 1;
                                }
                            }

                            if ((int)start.Y != (int)target.Y)
                            {
                                if ((int)start.Y < (int)target.Y)
                                {
                                    _PointA = start;
                                    _PointB = target;
                                }

                                if ((int)start.Y > (int)target.Y)
                                {
                                    _PointA = target;
                                    _PointB = start;
                                }

                                while ((int)_PointA.Y <= (int)_PointB.Y)
                                {
                                    setPositionData(requestData, _PointA, walkable, 0, 0);
                                    _PointA.Y = (int)_PointA.Y + 1;
                                }
                            }

                            m_scriptModule.DispatchReply(requestData.ScriptID, 19850, "", requestData.RequestID.ToString());
                            return;
                        }
                        else
                        {
                            m_scriptModule.DispatchReply(requestData.ScriptID, 19853, "", requestData.RequestID.ToString());
                        }
                    }
                    else
                    {
                        m_scriptModule.DispatchReply(requestData.ScriptID, 19851, "", requestData.RequestID.ToString());
                    }
                }
                catch (Exception _error)
                {
                    m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
                }
            }
        }

        private void generatePath(ScriptRequestData requestData)
        {
            try
            {
                lock (m_environments)
                {
                    Environment _env = m_environments.Find(X => X.ID == requestData.EnvironmentID);

                    if (_env.Start != null && _env.Target != null)
                    {
                        if (_env.Start.PositionX == _env.Target.PositionX && _env.Start.PositionY == _env.Target.PositionY)
                            m_scriptModule.DispatchReply(requestData.ScriptID, 19850, "", requestData.RequestID.ToString());

                        GridSize _pathFindingGridSize = new GridSize(_env.Size, _env.Size);
                        Roy_T.AStar.Primitives.Size _pathFindingCellSize = new Roy_T.AStar.Primitives.Size(Distance.FromMeters(1), Distance.FromMeters(1));
                        Velocity _pathFindingVelocity = Velocity.FromKilometersPerHour(11.5f);

                        Grid _pathFindingGrid = Grid.CreateGridWithLateralAndDiagonalConnections(_pathFindingGridSize, _pathFindingCellSize, _pathFindingVelocity);

                        foreach (INode _thisNode in _pathFindingGrid.GetAllNodes())
                        {
                            PathNode _node = _env.Nodes.Find(X => X.PositionX == (int)_thisNode.Position.X && X.PositionY == (int)_thisNode.Position.Y);

                            if (_node == null)
                            {
                                _pathFindingGrid.DisconnectNode(new GridPosition((int)_thisNode.Position.X, (int)_thisNode.Position.Y));
                                _pathFindingGrid.RemoveDiagonalConnectionsIntersectingWithNode(new GridPosition((int)_thisNode.Position.X, (int)_thisNode.Position.Y));
                            }
                        }

                        PathFinder pathFinder = new PathFinder();
                        Path _pathFindingPath = pathFinder.FindPath(new GridPosition(_env.Start.PositionX, _env.Start.PositionY), new GridPosition(_env.Target.PositionX, _env.Target.PositionY), _pathFindingGrid);

                        String _pathString = "";
                        int lastX = 0;
                        int lastY = 0;

                        foreach (var _thisEdge in _pathFindingPath.Edges)
                        {
                            if (lastX != (int)_thisEdge.End.Position.X && lastY != (int)_thisEdge.End.Position.Y)
                            {
                                _pathString += "<" + _thisEdge.End.Position.X + ", " + _thisEdge.End.Position.Y + ", 0>;";

                                lastX = (int)_thisEdge.End.Position.X;
                                lastY = (int)_thisEdge.End.Position.Y;
                            }
                        }

                        _pathString += "<" + _pathFindingPath.Edges[_pathFindingPath.Edges.Count - 1].End.Position.X + ", " + _pathFindingPath.Edges[_pathFindingPath.Edges.Count - 1].End.Position.Y + ", 0>;";


                        if (_pathFindingPath.Type == PathType.Complete)
                        {
                            m_scriptModule.DispatchReply(requestData.ScriptID, 19850, _pathString, requestData.RequestID.ToString());
                        }
                        else
                        {
                            m_scriptModule.DispatchReply(requestData.ScriptID, 19855, _pathString, requestData.RequestID.ToString());
                        }
                    }
                    else
                    {
                        m_scriptModule.DispatchReply(requestData.ScriptID, 19854, "", requestData.RequestID.ToString());
                    }
                }
            }catch(Exception _error)
            {
                m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
            }
        }

        private void generateDebugImage(ScriptRequestData requestData)
        {
            lock(m_environments)
            {
                try
                {
                    Environment _env = m_environments.Find(X => X.ID == requestData.EnvironmentID);

                    if (_env != null)
                    {
                        Bitmap _bitmap = new Bitmap(_env.Size, _env.Size);

                        foreach (PathNode thisNode in _env.Nodes)
                        {
                            if (thisNode.Walkable)
                                _bitmap.SetPixel(thisNode.PositionX, thisNode.PositionY, Color.Green);

                            if (!thisNode.Walkable)
                                _bitmap.SetPixel(thisNode.PositionX, thisNode.PositionY, Color.Black);
                        }

                        _bitmap.Save(requestData.EnvironmentID + ".png");
                        m_scriptModule.DispatchReply(requestData.ScriptID, 19850, requestData.EnvironmentID + ".png", requestData.RequestID.ToString());
                        return;
                    }

                    m_scriptModule.DispatchReply(requestData.ScriptID, 19851, "", requestData.RequestID.ToString());
                }
                catch (Exception _error)
                {
                    m_scriptModule.DispatchReply(requestData.ScriptID, 19860, _error.Message, requestData.RequestID.ToString());
                }
            }
        }

        #endregion

        #region Script Funktions

        [ScriptInvocation]
        public string osGeneratePathEnv(UUID hostID, UUID scriptID)
        {
            UUID requestKey = UUID.Random();
 
            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { generatePathEnvironment(new ScriptRequestData(hostID, scriptID, null, requestKey)); })).Start();

            return requestKey.ToString();
        }

        [ScriptInvocation]
        public string osKeepAlivePathEnv(UUID hostID, UUID scriptID, String environmentID)
        {
            UUID requestKey = UUID.Random();

            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { keepAlivePathEnv(new ScriptRequestData(hostID, scriptID, environmentID, requestKey)); })).Start();

            return requestKey.ToString();
        }

        [ScriptInvocation]
        public void osSetPathPositionData(UUID hostID, UUID scriptID, String environmentID, Vector3 position, int walkable, int isTarget, int isStart)
        {
            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { setPositionData(new ScriptRequestData(hostID, scriptID, environmentID), position, walkable, isTarget, isStart); })).Start();
        }

        [ScriptInvocation]
        public void osSetPathLineData(UUID hostID, UUID scriptID, String environmentID, Vector3 start, Vector3 target, int walkable)
        {
            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { setLineData(new ScriptRequestData(hostID, scriptID, environmentID), start, target, walkable); })).Start();
        }

        [ScriptInvocation]
        public string osGeneratePath(UUID hostID, UUID scriptID, String environmentID)
        {
            UUID requestKey = UUID.Random();

            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { generatePath(new ScriptRequestData(hostID, scriptID, environmentID, requestKey)); })).Start();

            return requestKey.ToString();
        }

        [ScriptInvocation]
        public string osGenerateDebugImage(UUID hostID, UUID scriptID, String environmentID)
        {
            UUID requestKey = UUID.Random();

            SceneObjectGroup _host = m_scene.GetSceneObjectGroup(hostID);
            (new Thread(delegate () { generateDebugImage(new ScriptRequestData(hostID, scriptID, environmentID, requestKey)); })).Start();

            return requestKey.ToString();
        }

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
