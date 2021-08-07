using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.ObjectFinder
{
    class ObjectData
    {
        public String Name = null;
        public UUID ID = UUID.Zero;
        public float Distance = 0;

        public ObjectData(String name, UUID id, float distance)
        {
            Name = name;
            ID = id;
            Distance = distance;
        }
    }
}
