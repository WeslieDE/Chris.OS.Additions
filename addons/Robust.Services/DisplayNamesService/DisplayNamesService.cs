using Chris.OS.Additions.Shared.Data.DisplayName;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Base;
using System;

namespace Chris.OS.Additions.Robust.Services.DisplayNamesService
{
    class DisplayNamesService : ServiceBase, IDisplayNameService
    {
        public DisplayNamesService(IConfigSource config, string configName) : base(config)
        {
        }

        public string getDisplayName(Uri displayNameServer, UUID user)
        {
            return "Christopher";
        }

        public bool setDisplayName(Uri displayNameServer, string newName)
        {
            return true;
        }
    }
}
