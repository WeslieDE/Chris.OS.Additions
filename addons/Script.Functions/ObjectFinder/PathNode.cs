using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.PathFinding
{
    class PathNode
    {
        public int PositionX = 0;
        public int PositionY = 0;

        public bool Walkable = false;

        public int f_cost = 99999;

        public PathNode Parent = null;

        public PathNode(int positionX, int positionY)
        {
            PositionX = positionX;
            PositionY = positionY;
        }

        public PathNode(int positionX, int positionY, bool isWalkable)
        {
            PositionX = positionX;
            PositionY = positionY;

            Walkable = isWalkable;
        }
    }
}
