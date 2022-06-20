using Chris.OS.Additions.Utils;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.MySQLClient
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "MySQLClient")]

    public class osMySQLClient : EmptyNonSharedModule
    {
        private List<iMySQLStorage> m_mySQLHandlers = new List<iMySQLStorage>();
        private String m_defaultConecctionString = null;
        private Timer m_timer = null;

        #region EmptyModule
        public override string Name
        {
            get { return "osMySQLClient"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.Logger.Info("[" + Name + "]: Load region " + scene.Name);

            base.World = scene;

            IScriptModuleComms scriptModuleComms = base.World.RequestModuleInterface<IScriptModuleComms>();
            if (scriptModuleComms != null)
            {
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCreateConecction");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCreateDefaultConecction");
                scriptModuleComms.RegisterScriptInvocation(this, "osSQLiteCreateConecction");

                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLOpenConecction");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCloseConecction");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLPingConecction");

                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCreateCommand");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandAddParameters");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLGetNextRow");

                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandExecute");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandExecuteNonQuery");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLGetError");
            }

            m_timer = new Timer();
            m_timer.Interval = 10000;
            m_timer.AutoReset = true;
            m_timer.Elapsed += cleanup;
            m_timer.Start();
        }

        public override void Initialise(IConfigSource source)
        {
            if (source.Configs["MySQL"] != null)
            {
                m_defaultConecctionString = source.Configs["MySQL"].GetString("SQLConnectionString", String.Empty).Trim();
            }
        }

        public override void Close()
        {
            m_timer.Stop();
            m_timer.Dispose();

            foreach (MySqlConnectionHandler handler in m_mySQLHandlers.FindAll(x => x.Connected == true))
            {
                handler.CloseConecction();
            }
        }

        private void cleanup(object sender, ElapsedEventArgs e)
        {
            foreach(MySqlConnectionHandler handler in m_mySQLHandlers.FindAll(x => x.Connected == true))
            {
                if ((handler.LastUse + 60) < Tools.getUnixTime())
                {
                    handler.CloseConecction();
                }
            }
        }
        #endregion

        #region Script Funktions
        [ScriptInvocation]
        public UUID osMySQLCreateConecction(UUID hostID, UUID scriptID, String connectionString)
        {
            if (connectionString != null && connectionString.Trim() != String.Empty)
            {
                UUID newHandlerID = UUID.Random();
                MySqlConnectionHandler newMySQLHandler = new MySqlConnectionHandler(connectionString, newHandlerID);
                m_mySQLHandlers.Add(newMySQLHandler);
                return newHandlerID;
            }

            return UUID.Zero;
        }

        [ScriptInvocation]
        public UUID osMySQLCreateDefaultConecction(UUID hostID, UUID scriptID)
        {
            if(m_defaultConecctionString != null && m_defaultConecctionString != String.Empty)
            {
                UUID newHandlerID = UUID.Random();
                MySqlConnectionHandler newMySQLHandler = new MySqlConnectionHandler(m_defaultConecctionString, newHandlerID);
                m_mySQLHandlers.Add(newMySQLHandler);
                return newHandlerID;
            }

            return UUID.Zero;
        }

        [ScriptInvocation]
        public UUID osMySQLOpenConecction(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.OpenConecction();
            }

            return conecctionID;
        }

        [ScriptInvocation]
        public void osMySQLPingConecction(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.PingConecction();
            }
        }

        [ScriptInvocation]
        public String osMySQLGetError(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                return conecctionHandler.Error;
            }

            return "CONECCTION_NOT_FOUND";
        }

        [ScriptInvocation]
        public UUID osMySQLCloseConecction(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CloseConecction();
            }

            return conecctionID;
        }

        [ScriptInvocation]
        public UUID osMySQLCreateCommand(UUID hostID, UUID scriptID, String command, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CreateCommand(command);
            }

            return conecctionID;
        }

        [ScriptInvocation]
        public void osMySQLCommandAddParameters(UUID hostID, UUID scriptID, String name, String value, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CommandAddParameters(name, value);
            }
        }

        [ScriptInvocation]
        public object[] osMySQLGetNextRow(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                return conecctionHandler.getNextRow();
            }

            return new object[0];
        }

        [ScriptInvocation]
        public int osMySQLCommandExecute(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                return conecctionHandler.CommandExecute();
            }

            return 0;
        }

        [ScriptInvocation]
        public int osMySQLCommandExecuteNonQuery(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            iMySQLStorage conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                return conecctionHandler.CommandExecuteNonQuery();
            }

            return 0;
        }
        #endregion
    }
}
