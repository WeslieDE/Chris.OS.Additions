using Newtonsoft.Json;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Net;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class HTTP : iStorage
    {
        private Scene m_scene = null;
        private String m_baseURL = null;

        public HTTP(Scene scene, String baseURL)
        {
            m_scene = scene;

            m_baseURL = baseURL;

            if (!checkServer())
                throw new Exception("Invalid Server");
        }

        public HTTP(Scene scene, IConfig config)
        {
            m_scene = scene;

            m_baseURL = config.GetString("Server", String.Empty).Trim();

            if (!checkServer())
                throw new Exception("Invalid Server");
        }

        public List<String> allIDs(string storageID)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers.Add("KeyValueMethode", "getAllIDs");
                    client.Headers.Add("KeyValueStorageID", storageID);

                    String response = client.DownloadString(m_baseURL);

                    if (response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);

                        if (serverData.ValidKeyValueResponse)
                            return serverData.KeyList;
                    }
                }
                catch
                {
                    return new List<String>();
                }

                return new List<String>();
            }
        }

        public bool check(string storageID, string key)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers.Add("KeyValueMethode", "check");
                    client.Headers.Add("KeyValueStorageID", storageID);
                    client.Headers.Add("KeyValueStorageKey", key);

                    String response = client.DownloadString(m_baseURL);

                    if (response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);

                        if (serverData.ValidKeyValueResponse)
                            return serverData.IsKeyValid;
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            }
        }

        public string get(string storageID, string key)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers.Add("KeyValueMethode", "get");
                    client.Headers.Add("KeyValueStorageID", storageID);
                    client.Headers.Add("KeyValueStorageKey", key);

                    String response = client.DownloadString(m_baseURL);

                    if (response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);

                        if (serverData.ValidKeyValueResponse)
                            return serverData.Value;
                    }
                }
                catch
                {
                    return String.Empty;
                }

                return String.Empty;
            }
        }

        public void remove(string storageID, string key)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers.Add("KeyValueMethode", "remove");
                    client.Headers.Add("KeyValueStorageID", storageID);
                    client.Headers.Add("KeyValueStorageKey", key);

                    String response = client.DownloadString(m_baseURL);

                    if (response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);

                        if (serverData.ValidKeyValueResponse)
                            return;
                    }
                }
                catch
                {
                    return;
                }

                return;
            }
        }

        public void save(string storageID, string key, string data)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers.Add("KeyValueMethode", "save");
                    client.Headers.Add("KeyValueStorageID", storageID);
                    client.Headers.Add("KeyValueStorageKey", key);

                    String response = client.UploadString(m_baseURL, data);

                    if (response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);

                        if (serverData.ValidKeyValueResponse)
                            return;
                    }
                }
                catch
                {
                    return;
                }

                return;
            }
        }

        private Boolean checkServer()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    String response = client.DownloadString(m_baseURL);

                    if(response != String.Empty)
                    {
                        HTTPServerResponse serverData = JsonConvert.DeserializeObject<HTTPServerResponse>(response);
                        return serverData.ValidKeyValueResponse;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    class HTTPServerResponse
    {
        public Boolean ValidKeyValueResponse = false;
        public Boolean IsKeyValid = false;
        public List<String> KeyList = new List<string>();
        public String Value = null;
    }
}
