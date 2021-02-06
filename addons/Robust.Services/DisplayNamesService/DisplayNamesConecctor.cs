using Chris.OS.Additions.Shared.Data.DisplayName;
using log4net;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Robust.Services.DisplayNamesService
{
    public class DisplayNamesConecctor : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Called from Robust
        public DisplayNamesConecctor(IConfigSource config, IHttpServer server, string configName) : this(config, server, configName, null)
        {
            m_log.Info("Loaded DisplayNamesConecctor (GRID)");
        }

        public DisplayNamesConecctor(IConfigSource config, IHttpServer server, string configName, IDisplayNameService localConn) : base(config, server, configName)
        {
            m_log.Info("Loaded DisplayNamesConecctor");

            IDisplayNameService m_service = new DisplayNamesService(config, "DisplayNames");

            server.AddStreamHandler(new DisplayNamesGETHandler(m_service, localConn));
        }
    }
}
