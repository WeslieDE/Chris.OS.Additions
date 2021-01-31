using Mono.Addins;
using OpenSim.Region.Framework.Interfaces;
using System;
using System.Collections.Generic;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using log4net;
using System.Reflection;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Servers;

namespace Chris.OS.Additions.Region.Modules.DataPublisher
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RegionsDataModule")]
    public class RegionsDataModule : INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfigSource m_config = null;
        private int m_httpport = 0;

        private List<Scene> m_scenes = new List<Scene>();

        public string Name
        {
            get
            {
                return "RegionsDataModule";
            }
        }


        public Type ReplaceableInterface
        {
            get
            {
                return null;
            }
        }


        public void AddRegion(Scene scene)
        {
            
        }

        public void Close()
        {

        }

        public void Initialise(IConfigSource source)
        {
            if (m_config == null)
            {
                m_config = source;

                if(m_config.Configs["Network"] != null)
                {
                    m_httpport = m_config.Configs["Network"].GetInt("http_listener_port", m_httpport);
                    m_log.Info("[" + Name + "] Start on Port " + m_httpport);
                }
                else
                {
                    m_log.Info("[" + Name + "] Cant find [Network].");
                }
            }
            else
            {
                m_log.Info("[" + Name + "] Cant read config.");
            }
        }

        public void PostInitialise()
        {
            
        }

        public void RegionLoaded(Scene scene)
        {
            m_log.Info("[" + Name + "] Load Region " + scene.Name);

            if (scene != null)
            {
                m_scenes.Add(scene);

                IHttpServer server = MainServer.GetHttpServer((uint)m_httpport);
                server.AddStreamHandler(new RegionsDataHandler(m_config, ref m_scenes));
                m_log.Info("["+ Name + "] Add RegionsDataHandler");
            }
        }


        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
        }
    }
}
