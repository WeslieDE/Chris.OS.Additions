using Chris.OS.Additions.Shared.Data.DisplayName;
using OpenMetaverse;
using System;

namespace Chris.OS.Additions.Region.Modules.DisplayNames
{
    public interface IDisplayNameManagement
    {
        String getDisplayName(UUID user);
        Boolean setDisplayName(UUID user, String name);
    }
}
