using Chris.OS.Additions.Utils;
using Nini.Config;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.IncommingChat
{
    class IncommingChatModule : EmptyModule
    {
        private int m_httpport = 0;

        #region EmptyModule
        public override string Name
        {
            get { return "IncommingChat"; }
        }

        public override void Initialise(IConfigSource source)
        {
            if (base.Config == null)
                base.Config = source;


            if (base.Config.Configs["Network"] != null)
            {
                m_httpport = base.Config.Configs["Network"].GetInt("http_listener_port", m_httpport);
            }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            if (base.World != null)
            {
                IHttpServer server = MainServer.GetHttpServer((uint)m_httpport);
                server.AddStreamHandler(new IncommingChatHandler(base.Config, ref base.World));
            }
        }
        #endregion
    }
}
