using Chris.OS.Additions.Shared.Data.DisplayName;
using log4net;
using Newtonsoft.Json;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.ServiceAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace Chris.OS.Additions.Robust.Services.DisplayNamesService
{
    class DisplayNamesGETHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IDisplayNameService m_DisplayNameService = null;

        public DisplayNamesGETHandler(IDisplayNameService service) : base("GET", "/displayname")
        {
            m_DisplayNameService = service;
        }

        public DisplayNamesGETHandler(IDisplayNameService service, IDisplayNameService localConn) : base("GET", "/displayname")
        {
            m_DisplayNameService = service;
        }

        public DisplayNamesGETHandler(IDisplayNameService service, IServiceAuth auth, string redirectURL) : base("GET", "/displayname", auth)
        {
            m_DisplayNameService = service;
        }

        protected override byte[] ProcessRequest(string path, Stream request, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            Dictionary<string, object> requestParams = new Dictionary<string, object>();
            foreach (string name in httpRequest.QueryString)
            {
                m_log.Info("[DISPLAY NAMES SERVICE][DEBUG] "+ name + " == " + httpRequest.QueryString[name]);

                requestParams[name] = httpRequest.QueryString[name];

            }

            string METHOD = Convert.ToString(requestParams["METHOD"]);

            if(METHOD != null)
            {
                UUID userID = UUID.Zero;
                if (UUID.TryParse(Convert.ToString(requestParams["user"]), out userID))
                {
                    if (userID != null)
                    {
                        if (METHOD == "GET")
                        {
                            m_log.Info("[DISPLAY NAMES SERVICE][DEBUG] GET 'GET' REUQUEST");
                            DisplayNameData data = new DisplayNameData() { User = userID.ToString(), DisplayName = m_DisplayNameService.getDisplayName(null, userID) };

                            httpResponse.StatusCode = (int)HttpStatusCode.OK;
                            httpResponse.ContentType = "application/json";
                            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                        }

                        if (METHOD == "SET")
                        {
                            m_log.Info("[DISPLAY NAMES SERVICE][DEBUG] GET 'SET' REUQUEST");

                            string newName = Convert.ToString(requestParams["NEW"]);

                            if(newName != null)
                            {
                                DisplayNameData data = new DisplayNameData() { User = userID.ToString(), DisplayName = newName };
                                m_DisplayNameService.setDisplayName(null, newName);

                                httpResponse.StatusCode = (int)HttpStatusCode.OK;
                                httpResponse.ContentType = "application/json";
                                return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                            }
                        }
                    }
                }
            }


            httpResponse.StatusCode = (int)HttpStatusCode.NotFound;
            httpResponse.ContentType = "text/plain";
            return new byte[0];
        }
    }
}
