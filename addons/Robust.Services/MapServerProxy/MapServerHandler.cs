using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using System;

namespace Chris.OS.Additions.Robust.Services.MapServerProxy
{
    class MapImageProxyHandler : ServiceConnector
    {
        private string m_ConfigName = "MapImageService";

        public MapImageProxyHandler(IConfigSource config, IHttpServer server, string configName) : base(config, server, configName)
        {
            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", m_ConfigName));

            string remoteServer = serverConfig.GetString("RemoteMapServer", String.Empty);

            if (remoteServer == String.Empty)
                throw new Exception("No RemoteServer in config file");

            server.AddStreamHandler(new MapServerProxy(remoteServer));
        }
    }
}

