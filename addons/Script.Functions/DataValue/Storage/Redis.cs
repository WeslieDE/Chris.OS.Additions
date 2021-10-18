using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using System;
using System.Reflection;
using System.Text;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class Redis : iStorage
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        private RedisClient m_client = null;

        public Redis(Scene scene, IConfig config)
        {
            m_scene = scene;

            String host = config.GetString("Redis_Host", "127.0.0.1").Trim();
            int port = config.GetInt("Redis_Port", 6379);

            try
            {
                m_client = new RedisClient(host, port);

                IRedisSubscription events = m_client.CreateSubscription();
                events.OnMessage += message;
            }
            catch (Exception _error)
            {
                m_log.Error("[REDIS] " + _error.Message);
            }
        }

        private void message(string arg1, string arg2)
        {
            DataStorageEvents.onSetDataValue(arg1, arg1, arg2);
            Console.WriteLine("[DEBUG] " + arg1 + ";" + arg2);

        }

        public bool check(string storageID, string key)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            return m_client.Exists(storageID + "." + key) != 0;
        }

        public string get(string storageID, string key)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            String data = m_client.Get<String>(storageID + "." + key);

            if (data == null)
                return "";

            return data;
        }

        public void remove(string storageID, string key)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            m_client.Remove(storageID + "." + key); 
        }

        public void save(string storageID, string key, string data)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            m_client.SetValue(storageID + "." + key, data);
        }
    }
}
