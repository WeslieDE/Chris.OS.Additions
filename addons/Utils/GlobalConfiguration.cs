using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Utils
{
    class GlobalConfiguration
    {
        public static String UpdateURL = "https://files.clatza.dev/OpenSimulator/Chris.OS.Additions.dll";
        public static String UpdateCheckURL = "https://files.clatza.dev/OpenSimulator/checkForUpdate.php?version=" + Utils.Version.MODULE_VERSION;

        public static Boolean PrintMOTD = true;
        public static String MOTH = " ====================\nChris OS Additions version " + Utils.Version.MODULE_VERSION + "\n ====================\n";
    }
}
