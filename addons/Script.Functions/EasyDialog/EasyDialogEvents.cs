using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.EasyDialog
{
    class EasyDialogEvents
    {
        #region Events
        public static Action<int, UUID> onDialogTimeout;
        #endregion
    }
}
