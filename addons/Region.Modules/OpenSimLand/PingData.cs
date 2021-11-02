using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Region.Modules.OpenSimLand
{
    class PingData
    {
        public String RegionName = null;
        public String UUID = null;

        public String Hostname = null;
        public int Port = 0;

        public String RegionOwnerID = null;
        public String RegionOwnerName = null;
        public String RegionOwnerMail = null;
        public Dictionary<string, object> RegionOwnerURL = null;

        public String GridName = null;
        public String HomeURL = null;
    }
}