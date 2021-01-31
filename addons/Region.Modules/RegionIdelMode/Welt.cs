using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Chris.OS.Additions.Region.Modules.RegionIdelMode
{
    class Welt
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        public bool InUse = false;

        private bool m_scriptsEnabled = true;

        private Dictionary<UUID, XmlDocument> m_states = new Dictionary<UUID, XmlDocument>();
        private List<NPCData> m_npcs = new List<NPCData>();

        public Welt(Scene _scene)
        {
            m_scene = _scene;
        }

        public String GetExtraSetting(String index)
        {
            return m_scene.GetExtraSetting(index);
        }

        public void StoreExtraSetting(String index, String data)
        {
            m_scene.StoreExtraSetting(index, data);
        }

        private void recreateNPC()
        {
            INPCModule module = m_scene.RequestModuleInterface<INPCModule>();

            foreach (NPCData data in m_npcs)
            {
                module.CreateNPC(data.FirstName, data.LastName, data.Position, data.AgentID, data.Owner, data.GroupTitle, data.GroupUUID, data.SenseAsAgent, m_scene, data.Appearance);
            }

            m_npcs = new List<NPCData>();
        }

        public bool ScriptsRunning
        {
            get
            {
                return m_scene.ScriptsEnabled;
            }

            set
            {
                m_scene.ScriptsEnabled = value;
            }
        }

        private void deleteAllNPC()
        {
            m_npcs = new List<NPCData>();

            try
            {
                INPCModule module = m_scene.RequestModuleInterface<INPCModule>();

                if (module != null)
                {
                    m_scene.ForEachRootScenePresence(
                        delegate (ScenePresence ssp)
                        {
                            if (ssp.IsNPC)
                            {
                                NPCData data = new NPCData();
                                data.FirstName = ssp.Firstname;
                                data.LastName = ssp.Lastname;
                                data.Position = ssp.AbsolutePosition;
                                data.AgentID = ssp.UUID;
                                data.Owner = module.GetOwner(ssp.UUID);
                                data.GroupTitle = ssp.Grouptitle;
                                data.GroupUUID = UUID.Zero;
                                data.SenseAsAgent = !ssp.IsNPC;
                                data.Appearance = ssp.Appearance;
                                m_npcs.Add(data);

                                module.DeleteNPC(ssp.UUID, m_scene);
                                m_log.Info("Remove NPC " + ssp.Firstname + " " + ssp.Lastname);
                            }
                        }
                    );
                }
                else
                {
                    m_log.Error("WARNING: NOT FOUND AN NPC MODULE!");
                }
            }
            catch (Exception _error)
            {
                m_log.Error("FATA ERROR: " + _error.Message);
            }
        }

        public int getRealAvatars()
        {
            int _agents = 0;

            INPCModule module = m_scene.RequestModuleInterface<INPCModule>();

            if (module != null)
            {
                m_scene.ForEachRootScenePresence(
                    delegate (ScenePresence ssp)
                    {
                        if (!ssp.IsNPC)
                            _agents++;
                    }
                );
            }
            else
            {
                try
                {
                    m_scene.ForEachClient(c => { _agents++; });
                }
                catch (Exception _error)
                {
                    m_log.Error("FATAL ERROR: " + _error.Message);
                }
            }

            return _agents;

        }

        public bool LoginsEnabled
        {
            get
            {
                return m_scene.LoginsEnabled;
            }

            set
            {
                m_scene.LoginsEnabled = value;
            }
        }

        public bool PhysicsEnabled
        {
            get
            {
                return m_scene.PhysicsEnabled;
            }

            set
            {
                m_scene.PhysicsEnabled = value;
            }
        }

        public bool ScriptsEnabled
        {
            get { return m_scriptsEnabled; }

            set
            {
                try
                {
                    IScriptModule ScriptEngine = m_scene.RequestModuleInterface<IScriptModule>();

                    if (m_scriptsEnabled != value)
                    {
                        if (!value)
                        {
                            deleteAllNPC();

                            ScriptEngine.SaveAllState();

                            foreach (SceneObjectGroup group in m_scene.GetSceneObjectGroups())
                            {
                                foreach (SceneObjectPart part in group.Parts)
                                {
                                    foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                                    {
                                        if (item.Type == (int)AssetType.LSLText)
                                        {
                                            ScriptEngine.SuspendScript(item.ItemID);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            recreateNPC();

                            foreach (SceneObjectGroup group in m_scene.GetSceneObjectGroups())
                            {
                                foreach (SceneObjectPart part in group.Parts)
                                {
                                    foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                                    {
                                        if (item.Type == (int)AssetType.LSLText)
                                        {
                                            ScriptEngine.ResumeScript(item.ItemID);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    m_scriptsEnabled = value;
                }
                catch (Exception error)
                {
                    m_log.Error(error.Message);
                    m_log.Error(error.StackTrace);
                }
            }
        }
    }
}
