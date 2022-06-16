using Chris.OS.Additions.Utils;
using log4net;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.MySQLClient
{
    public class MySqlConnectionHandler
    {
        private readonly ILog m_logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


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
            try
            {
                if (m_currentDataReader != null)
                    m_currentDataReader.Close();

                m_currentDataReader = null;
                m_currentMySQLCommand = null;

                m_mySQLClient.Close();
            }catch(Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][CloseConecction()] " + error.Message);
            }
        }

        public void CreateCommand(String command)
        {
            try
            {
                if (m_currentMySQLCommand != null)
                {
                    m_currentMySQLCommand.Cancel();
                    m_currentMySQLCommand = null;
                }

                m_currentMySQLCommand = m_mySQLClient.CreateCommand();
                m_currentMySQLCommand.CommandText = command;
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][CreateCommand()] " + error.Message);
            }
        }

        public void CommandAddParameters(String name, String value)
        {
            try
            {
                if (m_currentMySQLCommand != null)
                    m_currentMySQLCommand.Parameters.AddWithValue(name, value);
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][CommandAddParameters()] " + error.Message);
            }
        }

        public void CommandExecute()
        {
            try
            {
                mysqlping();
                if (m_currentMySQLCommand != null)
                {
                    lock (m_mySQLClient)
                    {
                        if (m_currentDataReader != null)
                        {
                            m_currentDataReader.Close();
                            m_currentDataReader = null;
                        }

                        m_currentDataReader = m_currentMySQLCommand.ExecuteReader();
                        m_currentMySQLCommand = null;
                    }
                }
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][CommandExecute()] " + error.Message);
            }
        }

        public String[] getNextRow()
        {
            try
            {
                if (m_currentDataReader == null)
                    return new String[0];

                if (m_currentDataReader.Read())
                {
                    List<String> result = new List<String>();
                    for (int i = 0; i < m_currentDataReader.FieldCount; i++)
                    {
                        result.Add(m_currentDataReader.GetName(i));
                        result.Add(m_currentDataReader.GetString(i));
                    }

                    return result.ToArray();
                }

                m_currentDataReader.Close();
                m_currentDataReader = null;
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][getNextRow()] " + error.Message);
            }

            return new String[0];
        }

        public void CommandExecuteNonQuery()
        {
            try
            {
                mysqlping();

                lock (m_mySQLClient)
                {
                    m_currentMySQLCommand.ExecuteReader();
                }
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][CommandExecuteNonQuery()] " + error.Message);
            }
        }
        #endregion

        #region Helpers
        private void mysqlping()
        {
            try
            {
                m_lastUse = Tools.getUnixTime();

                lock (m_mySQLClient)
                {
                    if (!m_mySQLClient.Ping())
                        m_mySQLClient.Open();
                }
            }
            catch (Exception error)
            {
                m_logger.Error("[MySqlConnectionHandler][mysqlping()] " + error.Message);
            }
        }
        #endregion
    }
}
