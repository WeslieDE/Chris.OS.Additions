using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Connectors;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Chris.OS.Additions.Robust.Services.MapServerProxy
{
    class MapServerProxy : BaseStreamHandler
    {
        public static ManualResetEvent ev = new ManualResetEvent(true);

        private String m_remoteServer = null;

        public MapServerProxy(String remoteServer) : base("GET", "/map")
        {
            m_remoteServer = remoteServer;
        }

        protected override byte[] ProcessRequest(string path, Stream request, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            UUID scopeID = UUID.Zero;
            string format = string.Empty;

            IniConfigSource configuration = new IniConfigSource();
            configuration.AddConfig("MapImageService");
            configuration.Configs["MapImageService"].Set("MapImageServerURI", m_remoteServer);

            MapImageServicesConnector mapServiceModule = new MapImageServicesConnector();
            mapServiceModule.Initialise(configuration);

            string[] bits = path.Trim('/').Split(new char[] { '/' });

            if (bits.Length > 2)
                scopeID = UUID.Parse(bits[1]);

            byte[] result = mapServiceModule.GetMapTile(path.Trim('/'), scopeID, out format);

            if (result.Length > 0)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.OK;
                if (format.Equals(".png"))
                    httpResponse.ContentType = "image/png";
                else if (format.Equals(".jpg") || format.Equals(".jpeg"))
                    httpResponse.ContentType = "image/jpeg";

                return result;
            }

            httpResponse.StatusCode = (int)HttpStatusCode.NotFound;
            httpResponse.ContentType = "text/plain";
            return new byte[0];
        }
    }
}
