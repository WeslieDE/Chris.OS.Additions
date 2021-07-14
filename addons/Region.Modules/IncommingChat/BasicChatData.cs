using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.IncommingChat
{
    class BasicChatData
    {
        public String Name = null;
        public int Channel = 0;
        public String Message = null;
        public ChatType Type = ChatType.Normal;
        public Vector3 Position = new Vector3();
        public Boolean Agent = false;
    }
}
