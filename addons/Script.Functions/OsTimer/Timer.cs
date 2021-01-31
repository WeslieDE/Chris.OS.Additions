using log4net;
using OpenMetaverse;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.OsTimer
{
    class Timer : System.Timers.Timer
    {
        public UUID scriptID = UUID.Zero;

        public Timer(UUID id)
        {
            scriptID = id;
        }

        public void run(object sender, ElapsedEventArgs e)
        {
            OsTimer.ScriptEngine.PostScriptEvent(scriptID, "timer", new String[0]);
        }
    }
}
