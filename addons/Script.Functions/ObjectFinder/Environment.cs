using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.PathFinding
{
    class Environment
    {
        public String ID = null;

        public int Size = 0;
        public int LastTimeUsed = 0;

        public PathNode Start = null;
        public PathNode Target = null;

        private List<PathNode> m_nodes = new List<PathNode>();
        public List<PathNode> Nodes
        {
            get
            {
                LastTimeUsed = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                return m_nodes;
            }
            set
            {
                LastTimeUsed = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                m_nodes = value;
            }
        }

        public Environment(String envID, int size)
        {
            ID = envID;

            Size = size;

            LastTimeUsed = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
