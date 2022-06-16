using Chris.OS.Additions.Utils;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Data;

namespace Chris.OS.Additions.Script.Functions.MySQLClient
{
    public class MySqlConnectionHandler
    {
        private MySqlCommand m_currentMySQLCommand = null;
        private IDataReader m_currentDataReader = null;

        public MySqlConnectionHandler(String connectionString, UUID handlerID)
        {
            m_connectionString = connectionString;
            m_handlerID = handlerID;
            m_lastUse = Tools.getUnixTime();

            if (connectionString == String.Empty)
                return;
        }

        #region GetSetHandler
        private UUID m_handlerID = UUID.Zero;
        public UUID HandlerID
        {
            get
            {
                return m_handlerID;
            }
        }

        private String m_connectionString = null;
        public String ConnectionString
        {
            get
            {
                return m_connectionString;
            }
        }

        private MySqlConnection m_mySQLClient = null;
        public MySqlConnection Conecction
        {
            get
            {
                return m_mySQLClient;
            }
        }

        private int m_lastUse = 0;
        public int LastUse
        {
            get
            {
                return m_lastUse;
            }
        }
        #endregion

        #region MySQLCommands
        public void OpenConecction()
        {
            m_lastUse = Tools.getUnixTime();

            m_mySQLClient = new MySqlConnection(m_connectionString);
            m_mySQLClient.Open();
        }

        public void CloseConecction()
        {
            if(m_currentDataReader != null)
                m_currentDataReader.Close();

            m_currentDataReader = null;
            m_currentMySQLCommand = null;

            m_mySQLClient.Close();
        }

        public void CreateCommand(String command)
        {
            m_currentMySQLCommand = m_mySQLClient.CreateCommand();
            m_currentMySQLCommand.CommandText = command;
        }

        public void CommandAddParameters(String name, String value)
        {
            if (m_currentMySQLCommand != null)
                m_currentMySQLCommand.Parameters.AddWithValue(name, value);
        }

        public void CommandExecute()
        {
            mysqlping();

            lock (m_mySQLClient)
            {
                m_currentDataReader = m_currentMySQLCommand.ExecuteReader();
            }
        }

        public String[] getNextRow()
        {
            if (m_currentDataReader == null)
                return new String[0];

            if(m_currentDataReader.Read())
            {
                List<String> result = new List<String>();
                for (int i = 0; i < m_currentDataReader.FieldCount; i++)
                {
                    result.Add(m_currentDataReader.GetName(i));
                    result.Add(m_currentDataReader.GetString(i));
                }

                return result.ToArray();
            }

            return new String[0];
        }

        public void CommandExecuteNonQuery()
        {
            mysqlping();

            lock (m_mySQLClient)
            {
                m_currentMySQLCommand.ExecuteReader();
            }
        }
        #endregion

        #region Helpers
        private void mysqlping()
        {
            m_lastUse = Tools.getUnixTime();

            lock (m_mySQLClient)
            {
                if (!m_mySQLClient.Ping())
                    m_mySQLClient.Open();
            }
        }
        #endregion
    }
}
