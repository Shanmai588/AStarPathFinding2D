using UnityEngine;

namespace RTS.Pathfinding 
{
    // Strategy pattern: Interface for calculating tile traversal costs
    public interface ICostProvider 
    {
        // Calculate the cost for an agent to move onto a specific tile
        float CalculateCost(Tile tile, AStarNavAgent agent);
        
        // Check if a tile is traversable by this agent
        bool IsTraversable(Tile tile, AStarNavAgent agent);
    }
    
    // Default implementation that uses base tile costs
    public class DefaultCostProvider : ICostProvider 
    {
        public float CalculateCost(Tile tile, AStarNavAgent agent)
        {
            if (!tile.Walkable)
                return float.MaxValue;
                
            return tile.BaseCost;
        }
        
        public bool IsTraversable(Tile tile, AStarNavAgent agent)
        {
            return tile.Walkable;
        }
    }
    
    // Implementation that applies movement penalties in mud
    public class MudAwareCostProvider : ICostProvider 
    {
        private readonly float mudPenaltyMultiplier;
        
        public MudAwareCostProvider(float mudPenaltyMultiplier = 2.5f)
        {
            this.mudPenaltyMultiplier = mudPenaltyMultiplier;
        }
        
        public float CalculateCost(Tile tile, AStarNavAgent agent)
        {
            if (!tile.Walkable)
                return float.MaxValue;
                
            float cost = tile.BaseCost;
            
            // Apply mud penalty if the tile has the mud flag
            if (tile.HasFlag(Tile.FLAG_MUD))
            {
                cost *= mudPenaltyMultiplier;
            }
            
            return cost;
        }
        
        public bool IsTraversable(Tile tile, AStarNavAgent agent)
        {
            return tile.Walkable;
        }
    }
    
    // Implementation that causes agents to avoid poisonous tiles
    public class PoisonAvoidanceCostProvider : ICostProvider 
    {
        private readonly float poisonPenaltyMultiplier;
        
        public PoisonAvoidanceCostProvider(float poisonPenaltyMultiplier = 5.0f)
        {
            this.poisonPenaltyMultiplier = poisonPenaltyMultiplier;
        }
        
        public float CalculateCost(Tile tile, AStarNavAgent agent)
        {
            if (!tile.Walkable)
                return float.MaxValue;
                
            float cost = tile.BaseCost;
            
            // Apply poison penalty if the tile has the poison flag
            if (tile.HasFlag(Tile.FLAG_POISON))
            {
                cost *= poisonPenaltyMultiplier;
            }
            
            return cost;
        }
        
        public bool IsTraversable(Tile tile, AStarNavAgent agent)
        {
            return tile.Walkable;
        }
    }
    
    // Combined cost provider that takes into account multiple factors
    public class CombinedCostProvider : ICostProvider 
    {
        private readonly ICostProvider[] providers;
        
        public CombinedCostProvider(params ICostProvider[] providers)
        {
            this.providers = providers;
        }
        
        public float CalculateCost(Tile tile, AStarNavAgent agent)
        {
            if (!tile.Walkable)
                return float.MaxValue;
                
            float cost = tile.BaseCost;
            
            // Apply all cost providers
            foreach (var provider in providers)
            {
                float providerCost = provider.CalculateCost(tile, agent);
                if (providerCost == float.MaxValue)
                    return float.MaxValue;
                    
                // For other providers, multiply effects
                cost *= (providerCost / tile.BaseCost);
            }
            
            return cost;
        }
        
        public bool IsTraversable(Tile tile, AStarNavAgent agent)
        {
            if (!tile.Walkable)
                return false;
                
            // Check all providers
            foreach (var provider in providers)
            {
                if (!provider.IsTraversable(tile, agent))
                    return false;
            }
            
            return true;
        }
    }
}