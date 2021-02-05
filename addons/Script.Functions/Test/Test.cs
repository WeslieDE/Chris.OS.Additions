using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.Test
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "Test")]

    class Test : EmptyModule
    {
        public override String Name
        {
            get
            {
                return "Test";
            }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.Logger.Info("========= TEST WORKED =========");
        }
    }
}
