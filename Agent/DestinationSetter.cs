using UnityEngine;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Handles setting and managing destination targets for agents.
    /// </summary>
    public class DestinationSetter : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float destinationArrivalDistance = 0.1f;

        private Vector2 targetPosition;
        private bool hasTargetPosition;
        private bool destinationReached;

        // Events
        public System.Action<Vector2> OnDestinationSetEvent;
        public System.Action OnDestinationReachedEvent;
        public System.Action OnNoWalkableDestinationEvent;

        private void Awake()
        {
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        }

        /// <summary>
        /// Set a new target position for the agent to move to.
        /// </summary>
        public void SetDestination(Vector2 position)
        {
            targetPosition = position;
            hasTargetPosition = true;
            destinationReached = false;

            // Find nearest walkable tile if the target isn't walkable
            Vector2Int targetGrid = gridManager.WorldToGrid(position);
            int goalIndex = gridManager.GridToIndex(targetGrid);

            if (!gridManager.GetTile(goalIndex).Walkable)
            {
                goalIndex = gridManager.FindNearestWalkableTile(goalIndex);

                // If no walkable tile found, we can't path
                if (goalIndex < 0)
                {
                    Debug.LogWarning($"No walkable tile found near target for {gameObject.name}");
                    OnNoWalkableDestinationEvent?.Invoke();
                    return;
                }

                // Update target position to the nearest walkable tile
                targetPosition = gridManager.GridToWorld2D(gridManager.IndexToGrid(goalIndex));
            }

            // Notify that a new destination has been set
            OnDestinationSetEvent?.Invoke(targetPosition);
        }

        /// <summary>
        /// Set a new target position for the agent to move to (Vector3 overload).
        /// </summary>
        public void SetDestination(Vector3 position)
        {
            SetDestination(new Vector2(position.x, position.y));
        }

        /// <summary>
        /// Called when the agent has reached its destination.
        /// </summary>
        public void OnDestinationReached()
        {
            destinationReached = true;
            OnDestinationReachedEvent?.Invoke();
        }

        /// <summary>
        /// Stop following the current path.
        /// </summary>
        public void ClearDestination()
        {
            hasTargetPosition = false;
            destinationReached = false;
        }

        /// <summary>
        /// Get the current destination.
        /// </summary>
        public Vector2 GetDestination()
        {
            return targetPosition;
        }

        /// <summary>
        /// Check if the agent has a destination set.
        /// </summary>
        public bool HasDestination()
        {
            return hasTargetPosition;
        }

        /// <summary>
        /// Check if the agent has reached its destination.
        /// </summary>
        public bool HasReachedDestination()
        {
            return destinationReached;
        }

        private void OnDrawGizmos()
        {
            // Draw a circle at the destination
            if (hasTargetPosition)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(new Vector3(targetPosition.x, targetPosition.y, transform.position.z), 0.3f);
            }
        }
    }
}