using Chris.OS.Additions.Utils;
using Mono.Addins;
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
        private List<MySqlConnectionHandler> m_mySQLHandlers = new List<MySqlConnectionHandler>();
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

                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLOpenConecction");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCloseConecction");
                
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCreateCommand");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandAddParameters");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLGetNextRow");

                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandExecute");
                scriptModuleComms.RegisterScriptInvocation(this, "osMySQLCommandExecuteNonQuery");
            }

            m_timer = new Timer();
            m_timer.Interval = 60000;
            m_timer.AutoReset = true;
            m_timer.Elapsed += cleanup;
            m_timer.Start();
        }

        public override void Close()
        {
            m_timer.Stop();
            m_timer.Dispose();

            foreach (MySqlConnectionHandler handler in m_mySQLHandlers)
            {
                handler.CloseConecction();
            }
        }

        private void cleanup(object sender, ElapsedEventArgs e)
        {
            foreach(MySqlConnectionHandler handler in m_mySQLHandlers)
            {
                if ((handler.LastUse + 300) < Tools.getUnixTime())
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
            UUID newHandlerID = UUID.Random();
            MySqlConnectionHandler newMySQLHandler = new MySqlConnectionHandler(connectionString, newHandlerID);
            m_mySQLHandlers.Add(newMySQLHandler);
            return newHandlerID;
        }

        [ScriptInvocation]
        public void osMySQLOpenConecction(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if(conecctionHandler != null)
            {
                conecctionHandler.OpenConecction();
            }
        }

        [ScriptInvocation]
        public void osMySQLCloseConecction(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CloseConecction();
            }
        }

        [ScriptInvocation]
        public void osMySQLCreateCommand(UUID hostID, UUID scriptID, String command, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CreateCommand(command);
            }
        }

        [ScriptInvocation]
        public void osMySQLCommandAddParameters(UUID hostID, UUID scriptID, String name, String value, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CommandAddParameters(name, value);
            }
        }

        [ScriptInvocation]
        public object[] osMySQLGetNextRow(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                return conecctionHandler.getNextRow();
            }

            return new object[0];
        }

        [ScriptInvocation]
        public void osMySQLCommandExecute(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CommandExecute();
            }
        }

        [ScriptInvocation]
        public void osMySQLCommandExecuteNonQuery(UUID hostID, UUID scriptID, UUID conecctionID)
        {
            MySqlConnectionHandler conecctionHandler = m_mySQLHandlers.Find(x => conecctionID == x.HandlerID);
            if (conecctionHandler != null)
            {
                conecctionHandler.CommandExecuteNonQuery();
            }
        }
        #endregion
    }
}
