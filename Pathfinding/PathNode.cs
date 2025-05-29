using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathNode
    {
        public Vector2Int position;
        public float gCost, hCost;
        public PathNode parent;

        public float FCost => gCost + hCost;

        public void Reset()
        {
            gCost = hCost = 0;
            parent = null;
        }
    }

}