using log4net;
using Newtonsoft.Json;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Chris.OS.Additions.Region.Modules.IncommingChat
{
    class IncommingChatHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_world = null;

        public IncommingChatHandler(IConfigSource config, ref Scene world) : base("POST", "/IncommingChat")
        {
            m_log.Info("[IncommingChatModule] IncommingChatHandler loaded.");

            m_world = world;
        }

        protected override byte[] ProcessRequest(string path, Stream requestData, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            try
            {
                String requestBody = new StreamReader(requestData).ReadToEnd();

                BasicChatData data = JsonConvert.DeserializeObject<BasicChatData>(requestBody);


                

                byte[] binText = Util.StringToBytesNoTerm(data.Message, 1023);
                m_world.SimChat(binText, ChatTypeEnum.Region, data.Channel, data.Position, data.Name, UUID.Random(), data.Agent);
                m_world.SimChatBroadcast(binText, ChatTypeEnum.Region, data.Channel, data.Position, data.Name, UUID.Random(), data.Agent);

                IWorldComm wComm = m_world.RequestModuleInterface<IWorldComm>();
                if (wComm != null)
                    wComm.DeliverMessage(ChatTypeEnum.Region, data.Channel, data.Name, UUID.Random(), Util.UTF8.GetString(binText));

                return Encoding.UTF8.GetBytes("ok");
            }catch(Exception error)
            {
                return Encoding.UTF8.GetBytes("error: " + error.Message);
            }
        }
    }
}
