using System;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using log4net;
using System.Reflection;

namespace Chris.OS.Additions.Robust.Services.FriendServerProxy
{
    public class FriendServerConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IUserAgentService m_UserAgentService;
        private IHGFriendsService m_TheService;
        private string m_ConfigName = "HGFriendsService";

        // Called from Robust
        public FriendServerConnector(IConfigSource config, IHttpServer server, string configName) : this(config, server, configName, null)
        {
            m_log.Info("Loaded FriendServerProxy (GRID)");
        }

        // Called from standalone configurations
        public FriendServerConnector(IConfigSource config, IHttpServer server, string configName, IFriendsSimConnector localConn) : base(config, server, configName)
        {
            if (configName != string.Empty)
                m_ConfigName = configName;

            Object[] args = new Object[] { config, m_ConfigName, localConn };

            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", m_ConfigName));

            string theService = serverConfig.GetString("LocalServiceModule",
                    String.Empty);
            if (theService == String.Empty)
                throw new Exception("No LocalServiceModule in config file");
            m_TheService = ServerUtils.LoadPlugin<IHGFriendsService>(theService, args);

            theService = serverConfig.GetString("UserAgentService", string.Empty);
            if (theService == String.Empty)
                throw new Exception("No UserAgentService in " + m_ConfigName);
            m_UserAgentService = ServerUtils.LoadPlugin<IUserAgentService>(theService, new Object[] { config, localConn });

            server.AddStreamHandler(new FriendServerProxy(m_TheService, m_UserAgentService, localConn));
        }
    }
}
