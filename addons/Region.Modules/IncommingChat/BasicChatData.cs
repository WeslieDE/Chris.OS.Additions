using OpenMetaverse;
using System;

namespace Chris.OS.Additions.Region.Modules.IncommingChat
{
    class BasicChatData
    {
        public String Name = null;
        public int Channel = 0;
        public String Message = null;
        public Vector3 Position = new Vector3();
        public Boolean Agent = false;
    }
}
