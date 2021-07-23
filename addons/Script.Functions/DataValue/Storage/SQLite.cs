using log4net;
using Mono.Data.SqliteClient;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class SQLite : iStorage
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private SqliteConnection m_connection = null;

        public SQLite(Scene scene, IConfig config)
        {
            try
            {
                m_connection = new SqliteConnection("URI=file:DataValue-" + scene.RegionInfo.RegionID.ToString() + ".db");

                createEmptyTable();
            }
            catch (Exception _error)
            {
                m_log.Error("[SQLITE] " + _error.Message);
            }
        }

        private void createEmptyTable()
        {
            lock (m_connection)
            {
                using (SqliteCommand _sqlCommand = new SqliteCommand())
                {
                    _sqlCommand.CommandText = "CREATE TABLE IF NOT EXISTS `StorageData` (`StorageID` VARCHAR(36) NOT NULL, `StorageKey` VARCHAR(512) NOT NULL, `StorageData` TEXT NOT NULL DEFAULT '', PRIMARY KEY(`StorageID`, `StorageKey`)) COLLATE = 'utf8_general_ci'; ";
                    _sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public bool check(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public string get(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public void remove(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public void save(string storageID, string key, string data)
        {
            throw new NotImplementedException();
        }
    }
}
