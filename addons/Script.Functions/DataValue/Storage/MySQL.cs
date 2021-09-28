using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Data;
using System.Reflection;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class MySQL : iStorage
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        private Timer m_timer = null;
        private String m_connectionString = null;

        private MySqlConnection m_mySQLClient = null;

        public MySQL(Scene scene, IConfig config)
        {
            m_scene = scene;

            m_connectionString = config.GetString("DataValueConnectionString", String.Empty).Trim();

            if (m_connectionString == String.Empty)
                return;

            m_timer = new Timer();
            m_timer.Interval = 10000;
            m_timer.Elapsed += mysqlping;
            m_timer.Start();

            try
            {
                m_mySQLClient = new MySqlConnection(m_connectionString);
                m_mySQLClient.Open();
                createEmptyTable();
            }
            catch(Exception _error)
            {
                m_log.Error("[MySQL] " + m_connectionString + " : " + _error.Message);
            }
        }

        private void mysqlping(object sender, ElapsedEventArgs e)
        {
            lock(m_mySQLClient)
            {
                if (!m_mySQLClient.Ping())
                    m_mySQLClient.Open();
            }
        }

        public bool check(String storageID, string key)
        {
            lock(m_mySQLClient)
            {
                bool returnValue = false;

                using (MySqlCommand _mysqlCommand = m_mySQLClient.CreateCommand())
                {
                    _mysqlCommand.CommandText = "Select StorageID, StorageKey FROM StorageData WHERE StorageID = ?mysqlStorage AND StorageKey = ?mysqlStorageKey";
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorage", storageID);
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorageKey", key);

                    using (IDataReader _mysqlReader = _mysqlCommand.ExecuteReader())
                    {
                        if (!_mysqlReader.Read())
                            _mysqlReader.Close();

                        if (_mysqlReader["StorageKey"] != null)
                            returnValue = true;

                        _mysqlReader.Close();
                        return returnValue;
                    }
                }
            }

        }

        public string get(String storageID, string key)
        {
            lock(m_mySQLClient)
            {
                String returnValue = null;

                using (MySqlCommand _mysqlCommand = m_mySQLClient.CreateCommand())
                {
                    _mysqlCommand.CommandText = "Select StorageID, StorageKey, StorageData FROM StorageData WHERE StorageID = ?mysqlStorage AND StorageKey = ?mysqlStorageKey";
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorage", storageID);
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorageKey", key);

                    using (IDataReader _mysqlReader = _mysqlCommand.ExecuteReader())
                    {
                        if (!_mysqlReader.Read())
                        {
                            _mysqlReader.Close();
                            return null;
                        }

                        if (_mysqlReader["StorageData"] != null)
                            returnValue = _mysqlReader["StorageData"].ToString();

                        _mysqlReader.Close();
                        return returnValue;
                    }
                }
            }
        }

        public void remove(string storageID, string key)
        {
            lock(m_mySQLClient)
            {
                using (MySqlCommand _mysqlCommand = m_mySQLClient.CreateCommand())
                {
                    _mysqlCommand.CommandText = "DELETE FROM StorageData WHERE StorageID = ?mysqlStorage AND StorageKey = ?mysqlStorageKey";
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorage", storageID);
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorageKey", key);
                    _mysqlCommand.ExecuteNonQuery();
                }
            }
        }

        public void save(String storageID, string key, string data)
        {
            lock(m_mySQLClient)
            {
                using (MySqlCommand _mysqlCommand = m_mySQLClient.CreateCommand())
                {
                    _mysqlCommand.CommandText = "REPLACE INTO StorageData (StorageID, StorageKey, StorageData) VALUES (?mysqlStorage, ?mysqlStorageKey, ?mysqlStorageData)";
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorage", storageID);
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorageKey", key);
                    _mysqlCommand.Parameters.AddWithValue("?mysqlStorageData", data);
                    _mysqlCommand.ExecuteNonQuery();
                }
            }
        }

        private void createEmptyTable()
        {
            lock(m_mySQLClient)
            {
                using (MySqlCommand _mysqlCommand = m_mySQLClient.CreateCommand())
                {
                    _mysqlCommand.CommandText = "CREATE TABLE IF NOT EXISTS `StorageData` (`StorageID` VARCHAR(36) NOT NULL, `StorageKey` VARCHAR(512) NOT NULL, `StorageData` LONGTEXT NOT NULL DEFAULT '', PRIMARY KEY(`StorageID`, `StorageKey`)) COLLATE = 'utf8_general_ci'; ";
                    _mysqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
