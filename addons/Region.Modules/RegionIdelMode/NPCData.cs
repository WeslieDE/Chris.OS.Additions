using OpenMetaverse;
using OpenSim.Framework;
using System;

namespace Chris.Os.Additions.RegionIdelMode
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
