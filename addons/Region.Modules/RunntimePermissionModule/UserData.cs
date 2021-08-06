using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Region.Modules.RunntimePermissionModule
{
    class UserData
    {
        public UUID User = UUID.Zero;
        public Dictionary<String, String> Rules = new Dictionary<string, string>();
    }
}
