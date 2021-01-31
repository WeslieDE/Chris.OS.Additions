using System;
using System.IO;
using Nini.Config;
using OpenSim.Framework.ServiceAuth;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Robust.AssetServerClient;
using log4net;
using System.Reflection;

namespace OpenSim.Server.Handlers.Asset
{
    public class NEWAssetServerConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IAssetService m_AssetService;
        private string m_ConfigName = "AssetService";

        // Called from Robust
        public NEWAssetServerConnector(IConfigSource config, IHttpServer server, string configName) : this(config, server, configName, null)
        {
            m_log.Info("Loaded NEWAssetServerConnector (GRID)");
        }

        // Called from standalone configurations
        public NEWAssetServerConnector(IConfigSource config, IHttpServer server, string configName, IFriendsSimConnector localConn) : base(config, server, configName)
        {
            if (configName != String.Empty)
                m_ConfigName = configName;

            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section '{0}' in config file", m_ConfigName));

            m_AssetService = new AssetServerProxy(config, m_ConfigName);

            IServiceAuth auth = ServiceAuth.Create(config, m_ConfigName);

            server.AddStreamHandler(new AssetServerGetHandler(m_AssetService, auth, string.Empty));
            server.AddStreamHandler(new AssetServerPostHandler(m_AssetService, auth));
            server.AddStreamHandler(new AssetServerDeleteHandler(m_AssetService, AllowedRemoteDeleteTypes.None, auth));
            server.AddStreamHandler(new AssetsExistHandler(m_AssetService));
        }
    }
}
