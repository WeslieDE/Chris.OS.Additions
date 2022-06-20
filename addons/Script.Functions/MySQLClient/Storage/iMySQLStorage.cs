using OpenMetaverse;
using System;

namespace Chris.OS.Additions.Script.Functions.MySQLClient
{
    internal interface iMySQLStorage
    {
        UUID HandlerID
        {
            get;
        }

        Boolean Connected
        {
            get;
        }

        String Error
        {
            get;
        }

        String ConnectionString
        {
            get;
        }

        int LastUse
        {
            get;
        }

        void OpenConecction();
        void CloseConecction();
        void PingConecction();
        void CreateCommand(String command);
        void CommandAddParameters(String name, String value);
        int CommandExecute();
        String[] getNextRow();
        int CommandExecuteNonQuery();

    }
}
