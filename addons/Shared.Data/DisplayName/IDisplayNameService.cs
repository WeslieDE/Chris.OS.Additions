using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Shared.Data.DisplayName
{
    public interface IDisplayNameService
    {
        String getDisplayName(Uri displayNameServer, UUID user);
        Boolean setDisplayName(Uri displayNameServer, String newName);
    }
}
