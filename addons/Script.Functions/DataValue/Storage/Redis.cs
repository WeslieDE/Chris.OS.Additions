﻿using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using ServiceStack.Redis;
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
            }
            catch (Exception _error)
            {
                m_log.Error("[REDIS] " + _error.Message);
            }
        }

        public bool check(string storageID, string key)
        {
            return m_client.Exists(storageID + "." + key) != 0;
        }

        public string get(string storageID, string key)
        {
            return Encoding.UTF8.GetString(m_client.Get(storageID + "." + key));
        }

        public void remove(string storageID, string key)
        {
            m_client.Remove(storageID + "." + key);
        }

        public void save(string storageID, string key, string data)
        {
            m_client.Add<String>(storageID + "." + key, data);
        }
    }
}
