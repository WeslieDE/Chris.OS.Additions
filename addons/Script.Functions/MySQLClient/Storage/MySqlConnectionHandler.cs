using Chris.OS.Additions.Utils;
using log4net;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Chris.OS.Additions.Script.Functions.MySQLClient
{
    public class MySqlConnectionHandler : iMySQLStorage
    {
        private readonly ILog m_logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        private MySqlCommand m_currentMySQLCommand = null;
        private IDataReader m_currentDataReader = null;

        private MySqlConnection m_mySQLClient = null;

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

        private Boolean m_connected = false;
        public Boolean Connected
        {
            get
            {
                return m_connected;
            }
        }

        private String m_errorMessage = "";
        public String Error
        {
            get
            {
                return m_errorMessage;
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

            m_connected = true;
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

                m_connected = false;
            }
            catch(Exception error)
            {
                throw new ScriptException(error.Message);
            }
        }

        public void PingConecction()
        {
            mysqlping();
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
                throw new ScriptException(error.Message);
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
                throw new ScriptException(error.Message);
            }
        }

        public int CommandExecute()
        {
            try
            {
                if (m_currentMySQLCommand != null)
                {
                    lock (m_mySQLClient)
                    {
                        if (m_currentDataReader != null)
                        {
                            m_currentDataReader.Close();
                            m_currentDataReader = null;
                        }

                        mysqlping();
                        m_currentDataReader = m_currentMySQLCommand.ExecuteReader();
                        m_currentMySQLCommand = null;
                    }
                }
            }
            catch (Exception error)
            {
                m_errorMessage = error.Message;
                return 0;
            }

            return 1;
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

        public int CommandExecuteNonQuery()
        {
            try
            {
                lock (m_mySQLClient)
                {
                    if (m_currentDataReader != null)
                    {
                        m_currentDataReader.Close();
                        m_currentDataReader = null;
                    }

                    mysqlping();
                    m_currentMySQLCommand.ExecuteNonQuery();
                }
            }
            catch (Exception error)
            {
                m_errorMessage = error.Message;
                m_logger.Error("[MySqlConnectionHandler][CommandExecuteNonQuery()] " + error.Message);
                return 0;
            }

            return 1;
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
