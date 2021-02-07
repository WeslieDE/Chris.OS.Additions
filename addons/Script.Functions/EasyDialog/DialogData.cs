using Chris.OS.Additions.Utils;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Script.Functions.EasyDialog
{
    class DialogData
    {
        #region Variablen
        private UUID m_hostID = UUID.Zero;
        public UUID HostID
        {
            get
            {
                return m_hostID;
            }
        }

        private UUID m_scriptID = UUID.Zero;
        public UUID ScriptID
        {
            get
            {
                return m_scriptID;
            }
        }

        private UUID m_userID = UUID.Zero;
        public UUID UserID
        {
            get
            {
                return m_userID;
            }
        }

        private UUID m_ownerID = UUID.Zero;
        public UUID OwnerID
        {
            get
            {
                return m_ownerID;
            }
        }

        private int m_listenerID = 0;
        public int ListenerID
        {
            get
            {
                return m_listenerID;
            }
        }

        private int m_lastTimeUsed = 0;
        public int LastTimeUsed
        {
            get
            {
                return m_lastTimeUsed;
            }
        }

        public String m_objectName = String.Empty;
        public String ObjectName
        {
            get
            {
                return m_objectName;
            }
        }

        public String m_message = String.Empty;
        public String Message
        {
            get
            {
                return m_message;
            }
        }

        public int m_currentPage = 0;
        public int CurrentPage
        {
            get
            {
                return m_currentPage;
            }

            set
            {
                if (value < 0)
                {
                    m_currentPage = (Buttons.Count-1) / 9;
                }
                else if (value > ((Buttons.Count - 1) / 9))
                {
                    m_currentPage = 0;
                }
                else
                {
                    m_currentPage = value;
                }
            }
        }

        private List<String> Buttons = new List<String>();

        #endregion

        public DialogData(UUID host, UUID script, UUID user, UUID owner, String name, String message, String[] buttons)
        {
            Random rnd = new Random();
            m_listenerID = rnd.Next(int.MinValue, int.MaxValue);

            m_hostID = host;
            m_scriptID = script;
            m_userID = user;
            m_ownerID = owner;
            m_objectName = name;

            m_message = message;
            Buttons = new List<String>(buttons);

            m_lastTimeUsed = Tools.getUnixTime();
        }

        public String[] getPageButtons(int page)
        {
            m_lastTimeUsed = Tools.getUnixTime();

            List<String> labels = new List<String>(new String[] { "<--", "<- EXIT ->", "-->" });

            int element = 9;
            if (Buttons.Count < element)
                element = Buttons.Count;

            if ((Buttons.Count - (9 * page)) < 9)
                element = (Buttons.Count - (9 * page));

            labels.AddRange(Buttons.GetRange((9 * page), element));

            while (labels.Count < 12)
                labels.Add(" ");

            return labels.ToArray();
        }

        public String getMessage()
        {
            String message = m_message;
            message += "\n \nPage: " + (m_currentPage+1) + " / " + (((Buttons.Count - 1) / 9)+1);
            return message;
        }
    }
}
