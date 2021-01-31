using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.PathFinding
{
    public class ScriptRequestData
    {
        public UUID HostID = UUID.Zero;
        public UUID ScriptID = UUID.Zero;
        public String EnvironmentID = null;
        public UUID RequestID = UUID.Zero;

        public ScriptRequestData(UUID host, UUID script)
        {
            HostID = host;
            ScriptID = script;
        }

        public ScriptRequestData(UUID host, UUID script, String envID)
        {
            HostID = host;
            ScriptID = script;
            EnvironmentID = envID;
        }

        public ScriptRequestData(UUID host, UUID script, String envID, UUID requestID)
        {
            HostID = host;
            ScriptID = script;
            EnvironmentID = envID;
            RequestID = requestID;
        }
    }
}
