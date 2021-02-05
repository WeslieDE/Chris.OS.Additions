using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.InternalRegionToScriptEvents
{
    enum ScriptEventTypes
    {
        EVENT_CUSTOM = 0,
        EVENT_NEWPRESENCE = 1,
        EVENT_REMOVEPRESENCE = 2,
        EVENT_AVATARENTERPARCEL = 3,
        EVENT_DATASTORAGESET = 1001,
        EVENT_DATASTORAGEREMOVE = 1002,
        EVENT_DATASTORAGERATELIMIT = 1003,
        EVENT_GENERIC = 42001337
    }
}
