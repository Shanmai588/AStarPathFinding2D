using UnityEngine;

namespace RTS.Pathfinding 
{
    // Navigation-focused agent class for use with the A* pathfinding system
    public class AStarNavAgent : MonoBehaviour 
    {
        [SerializeField] private float moveSpeed = 3.0f;
        
        // Terrain traversal capabilities
        [SerializeField] private bool canTraverseMud = true;
        [SerializeField] private bool canTraverseWater = false;
        [SerializeField] private bool isImmuneToPoison = false;
        
        // Properties
        public float MoveSpeed => moveSpeed;
        public bool CanTraverseMud => canTraverseMud;
        public bool CanTraverseWater => canTraverseWater;
        public bool IsImmuneToPoison => isImmuneToPoison;
        
        // Create a custom cost provider based on this agent's properties
        public ICostProvider CreateCostProvider()
        {
            // Create specialized cost providers based on agent properties
            var providers = new System.Collections.Generic.List<ICostProvider>();
            
            // Add mud handling if needed
            if (!canTraverseMud)
            {
                providers.Add(new MudAwareCostProvider(5.0f)); // High penalty for mud
            }
            
            // Add poison handling if needed
            if (!isImmuneToPoison)
            {
                providers.Add(new PoisonAvoidanceCostProvider());
            }
            
            // If no special handlers, use default
            if (providers.Count == 0)
            {
                return new DefaultCostProvider();
            }
            
            // Use combined provider if multiple strategies
            if (providers.Count > 1)
            {
                return new CombinedCostProvider(providers.ToArray());
            }
            
            // Just return the single provider
            return providers[0];
        }
        
        // Set the agent's move speed
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0.1f, speed);
        }
        
        // Set terrain traversal capabilities
        public void SetTerrainCapabilities(bool canTraverseMud, bool canTraverseWater, bool isImmuneToPoison)
        {
            this.canTraverseMud = canTraverseMud;
            this.canTraverseWater = canTraverseWater;
            this.isImmuneToPoison = isImmuneToPoison;
        }
    }
    
    
}