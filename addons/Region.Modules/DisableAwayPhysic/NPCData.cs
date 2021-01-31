using OpenMetaverse;
using OpenSim.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.DisableAwayPhysic
{
    class NPCData
    {
        public String FirstName = null;
        public String LastName = null;
        public Vector3 Position = Vector3.MaxValue;
        public UUID AgentID = UUID.Zero;
        public UUID Owner = UUID.Zero;
        public String GroupTitle = null;
        public UUID GroupUUID = UUID.Zero;
        public Boolean SenseAsAgent = false;
        public AvatarAppearance Appearance = null;
    }
}
