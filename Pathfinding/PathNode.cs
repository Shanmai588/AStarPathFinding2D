using System;
using System.Collections.Generic;

namespace RTS.Pathfinding 
{
    // Working record for a single node during A* search
    public struct PathNode : IComparable<PathNode>
    {
        // Grid index of this node
        public int GridIndex;
        
        // Cost from start to this node
        public float G;
        
        // Estimated cost from this node to goal
        public float H;
        
        // Parent node index in the path
        public int ParentIndex;
        
        // Total cost (F = G + H)
        public float F => G + H;
        
        // Constructor
        public PathNode(int gridIndex, float g, float h, int parentIndex)
        {
            GridIndex = gridIndex;
            G = g;
            H = h;
            ParentIndex = parentIndex;
        }
        
        // Comparison for priority queue
        public int CompareTo(PathNode other)
        {
            // First compare F values
            int fComparison = F.CompareTo(other.F);
            if (fComparison != 0)
                return fComparison;
                
            // If F values are equal, prefer lower H (closer to goal)
            return H.CompareTo(other.H);
        }
    }
    
    // Specialized priority queue for PathNodes
    public class PathNodePriorityQueue
    {
        private List<PathNode> nodes;
        private Dictionary<int, int> indexLookup; // Maps grid index to position in the list
        
        public PathNodePriorityQueue(int capacity = 1024)
        {
            nodes = new List<PathNode>(capacity);
            indexLookup = new Dictionary<int, int>(capacity);
        }
        
        public int Count => nodes.Count;
        
        public void Clear()
        {
            nodes.Clear();
            indexLookup.Clear();
        }
        
        // Add a node to the queue
        public void Enqueue(PathNode node)
        {
            // If node already exists, update it
            if (indexLookup.TryGetValue(node.GridIndex, out int existingIndex))
            {
                if (node.F < nodes[existingIndex].F)
                {
                    nodes[existingIndex] = node;
                    SiftUp(existingIndex);
                }
                return;
            }
            
            // Add new node
            nodes.Add(node);
            int index = nodes.Count - 1;
            indexLookup[node.GridIndex] = index;
            SiftUp(index);
        }
        
        // Get and remove the node with lowest F value
        public PathNode Dequeue()
        {
            if (nodes.Count == 0)
                throw new InvalidOperationException("Queue is empty");
                
            PathNode result = nodes[0];
            
            // Remove from the lookup
            indexLookup.Remove(result.GridIndex);
            
            // Replace root with the last element
            int lastIndex = nodes.Count - 1;
            if (lastIndex > 0)
            {
                nodes[0] = nodes[lastIndex];
                indexLookup[nodes[0].GridIndex] = 0;
                nodes.RemoveAt(lastIndex);
                SiftDown(0);
            }
            else
            {
                nodes.RemoveAt(lastIndex);
            }
            
            return result;
        }
        
        // Check if the queue contains a node with the given grid index
        public bool Contains(int gridIndex)
        {
            return indexLookup.ContainsKey(gridIndex);
        }
        
        // Get the node for a specific grid index without removing it
        public bool TryGetNode(int gridIndex, out PathNode node)
        {
            if (indexLookup.TryGetValue(gridIndex, out int index))
            {
                node = nodes[index];
                return true;
            }
            
            node = default;
            return false;
        }
        
        // Move a node up the heap until it's in the correct position
        private void SiftUp(int index)
        {
            PathNode node = nodes[index];
            
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (nodes[parentIndex].CompareTo(node) <= 0)
                    break;
                    
                // Swap with parent
                nodes[index] = nodes[parentIndex];
                indexLookup[nodes[index].GridIndex] = index;
                
                index = parentIndex;
            }
            
            nodes[index] = node;
            indexLookup[node.GridIndex] = index;
        }
        
        // Move a node down the heap until it's in the correct position
        private void SiftDown(int index)
        {
            int count = nodes.Count;
            PathNode node = nodes[index];
            
            while (true)
            {
                int childIndex = index * 2 + 1;
                if (childIndex >= count)
                    break;
                    
                // Choose the smaller child
                int rightChildIndex = childIndex + 1;
                if (rightChildIndex < count && nodes[rightChildIndex].CompareTo(nodes[childIndex]) < 0)
                    childIndex = rightChildIndex;
                    
                if (node.CompareTo(nodes[childIndex]) <= 0)
                    break;
                    
                // Swap with child
                nodes[index] = nodes[childIndex];
                indexLookup[nodes[index].GridIndex] = index;
                
                index = childIndex;
            }
            
            nodes[index] = node;
            indexLookup[node.GridIndex] = index;
        }
    }
}