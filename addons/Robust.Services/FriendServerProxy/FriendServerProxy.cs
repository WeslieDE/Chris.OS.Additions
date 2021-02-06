using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Hypergrid;

namespace Chris.OS.Additions.Robust.Services.FriendServerProxy
{
    public class FriendServerProxy : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IUserAgentService m_UserAgentService;
        private IFriendsSimConnector m_FriendsLocalSimConnector;
        private IHGFriendsService m_TheService;

        private HGFriendsServerPostHandler m_realHandler = null;

        List<String> m_fixCache = new List<String>();

        public FriendServerProxy(IHGFriendsService service, IUserAgentService uas, IFriendsSimConnector friendsConn) : base("POST", "/hgfriends")
        {
            m_TheService = service;
            m_UserAgentService = uas;
            m_FriendsLocalSimConnector = friendsConn;
            m_realHandler = new HGFriendsServerPostHandler(service, uas, friendsConn);

            if (File.Exists("friends.txt"))
                m_fixCache = new List<String>(File.ReadAllLines("friends.txt"));
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        
        protected override byte[] ProcessRequest(string path, Stream requestData, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            try
            {
                using (StreamReader _streamReader = new StreamReader(requestData))
                {
                    String body = _streamReader.ReadToEnd();
                    //m_log.Info("BODY: " + body);

                    Dictionary<string, object> request = ServerUtils.ParseQueryString(body);

                    if (request.ContainsKey("userID"))
                    {
                        string userID = request["userID"].ToString();

                        if (request.ContainsKey("METHOD"))
                        {
                            if(request["METHOD"].ToString() == "statusnotification")
                            {
                                for (int _friendID = 0; request.ContainsKey("friend_" + _friendID); _friendID++)
                                {
                                    String FriendData = userID + ";" + request["friend_" + _friendID].ToString().Trim();

                                    if (!m_fixCache.Contains(FriendData))
                                        m_fixCache.Add(FriendData);
                                }
                            }

                            if (request["METHOD"].ToString() == "getfullfriendsfile")
                            {
                                httpResponse.StatusCode = (int)200;
                                httpResponse.ContentType = "text/plain";

                                if(File.Exists("friends.txt"))
                                    return Encoding.ASCII.GetBytes(File.ReadAllText("friends.txt"));

                                return Encoding.ASCII.GetBytes("ERROR: friends.txt not found.");
                            }
                        }
                    }

                    lock(m_fixCache)
                    {
                        File.WriteAllLines("friends.txt", m_fixCache.ToArray());
                    }

                    return m_realHandler.Handle(path, GenerateStreamFromString(body), httpRequest, httpResponse);
                }
            }catch(Exception _e)
            {
                m_log.Info("EXAPTION: " + _e.Message);
                return null;
            }
        }
    }
}
