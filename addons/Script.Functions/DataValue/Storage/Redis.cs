﻿using log4net;
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
            }
            catch (Exception _error)
            {
                m_log.Error("[REDIS] " + _error.Message);
            }
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

            IRedisTypedClient<RedisDataElement> objClient = m_client.As<RedisDataElement>();
            RedisDataElement data = objClient.GetValue(storageID + "." + key);

            if (data == null)
                return "";

            return data.Data;
        }

        public void remove(string storageID, string key)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            m_client.Remove(storageID + "." + key);

            m_client.Save();
        }

        public void save(string storageID, string key, string data)
        {
            if (m_client == null)
                m_log.Error("[REDIS] client is null");

            RedisDataElement rsdata = new RedisDataElement(storageID + "." + key, data);
            IRedisTypedClient<RedisDataElement> objClient = m_client.As<RedisDataElement>();
            objClient.SetValue(storageID + "." + key, rsdata);
        }
    }

    public class RedisDataElement
    {
        public String Path = null;
        public String Data = null;

        public RedisDataElement(String path, String data)
        {
            Path = path;
            Data = data;
        }
    }
}
