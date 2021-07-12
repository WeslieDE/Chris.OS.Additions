using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.DiscordRelay
{
    public class WebHook
    {
        private String m_webhookURL = null;
        private WebHookData m_data = null;

        public WebHook(String url)
        {
            m_webhookURL = url;
            m_data = new WebHookData();
        }

        public void sendAsync()
        {
            new Thread(() =>{send();}).Start();
        }

        public void send()
        {
            if (m_data.content.Trim() == String.Empty)
                return;

            try
            {
                WebClient client = new WebClient();
                client.Headers["Content-Type"] = "application/json";
                client.Encoding = Encoding.UTF8;

                client.UploadString(m_webhookURL, JsonConvert.SerializeObject(m_data));

            }
            catch (Exception error)
            {
                Console.WriteLine("Error while making request to discord webhook.");
                Console.WriteLine(error.Message);
                Console.WriteLine(JsonConvert.SerializeObject(m_data));
            }
        }

        public String Name
        {
            get
            {
                return m_data.username;
            }

            set
            {
                m_data.username = value;
            }
        }

        public String Avatar
        {
            get
            {
                return m_data.avatar_url;
            }

            set
            {
                m_data.avatar_url = value;
            }
        }

        public String Message
        {
            get
            {
                return m_data.content;
            }

            set
            {
                m_data.content = value;
            }
        }
    }
}
