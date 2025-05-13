using UnityEngine;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Lightweight controller that coordinates the behavior of individual agent components.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        // Component references - automatically found in Awake if not set
        [SerializeField] private DestinationSetter destinationSetter;
        [SerializeField] private PathRequester pathRequester;
        [SerializeField] private PathFollower2D pathFollower;
        [SerializeField] private ReservationCoordinator reservationCoordinator;
        [SerializeField] private SpriteOrientation2D spriteOrientation;
        [SerializeField] private AStarNavAgent navAgent;

        // Events from original AgentController that external systems might depend on
        public System.Action OnDestinationReachedEvent;
        public System.Action OnPathFailedEvent;

        private void Awake()
        {
            // Find components if not assigned
            if (destinationSetter == null) destinationSetter = GetComponent<DestinationSetter>();
            if (pathRequester == null) pathRequester = GetComponent<PathRequester>();
            if (pathFollower == null) pathFollower = GetComponent<PathFollower2D>();
            if (reservationCoordinator == null) reservationCoordinator = GetComponent<ReservationCoordinator>();
            if (spriteOrientation == null) spriteOrientation = GetComponent<SpriteOrientation2D>();
            if (navAgent == null) navAgent = GetComponent<AStarNavAgent>();

            // Add any missing components
            if (destinationSetter == null) destinationSetter = gameObject.AddComponent<DestinationSetter>();
            if (pathRequester == null) pathRequester = gameObject.AddComponent<PathRequester>();
            if (pathFollower == null) pathFollower = gameObject.AddComponent<PathFollower2D>();
            if (reservationCoordinator == null) reservationCoordinator = gameObject.AddComponent<ReservationCoordinator>();
            if (spriteOrientation == null) spriteOrientation = gameObject.AddComponent<SpriteOrientation2D>();
        }

        private void OnEnable()
        {
            // Wire up events
            destinationSetter.OnDestinationSetEvent += HandleDestinationSet;
            destinationSetter.OnDestinationReachedEvent += HandleDestinationReached;
            destinationSetter.OnNoWalkableDestinationEvent += HandlePathFailed;
            
            pathRequester.OnPathFoundEvent += HandlePathFound;
            pathRequester.OnPathFailedEvent += HandlePathFailed;
            
            pathFollower.OnWaypointReachedEvent += HandleWaypointReached;
            pathFollower.OnPathCompletedEvent += HandlePathCompleted;
            pathFollower.OnWaypointIndexUpdatedEvent += HandleWaypointIndexUpdated;
        }

        private void OnDisable()
        {
            // Unwire events
            destinationSetter.OnDestinationSetEvent -= HandleDestinationSet;
            destinationSetter.OnDestinationReachedEvent -= HandleDestinationReached;
            destinationSetter.OnNoWalkableDestinationEvent -= HandlePathFailed;
            
            pathRequester.OnPathFoundEvent -= HandlePathFound;
            pathRequester.OnPathFailedEvent -= HandlePathFailed;
            
            pathFollower.OnWaypointReachedEvent -= HandleWaypointReached;
            pathFollower.OnPathCompletedEvent -= HandlePathCompleted;
            pathFollower.OnWaypointIndexUpdatedEvent -= HandleWaypointIndexUpdated;
        }

        private void Update()
        {
            // Check if we should repath
            if (pathFollower.IsMoving() && pathRequester.HasPath())
            {
                float remainingDistance = pathFollower.CalculateRemainingPathDistance();
                if (pathRequester.ShouldRepath(remainingDistance))
                {
                    pathRequester.RequestPath(destinationSetter.GetDestination());
                }
            }
        }

        // Event Handlers

        private void HandleDestinationSet(Vector2 destination)
        {
            // When a new destination is set, request a path to it
            pathRequester.RequestPath(destination);
        }

        private void HandlePathFound(List<Vector3> path, List<int> pathIndices, List<Door> doors)
        {
            // When a path is found, set it on the path follower
            pathFollower.SetPath(path, pathIndices, doors);
            
            // Get the current waypoint index from the path follower after it's been updated
            int currentWaypointIndex = pathFollower.GetCurrentWaypointIndex();
            
            // Update the reservation coordinator with the correct waypoint index
            reservationCoordinator.SetPath(pathIndices, currentWaypointIndex);
        }

        private void HandleWaypointReached()
        {
            // Handle any logic needed when a waypoint is reached
        }

        private void HandleWaypointIndexUpdated(int newIndex)
        {
            // Update the reservation coordinator with the new waypoint index
            reservationCoordinator.UpdateWaypointIndex(newIndex);
        }

        private void HandlePathCompleted()
        {
            // When the path is completed, inform the destination setter
            destinationSetter.OnDestinationReached();
        }

        private void HandleDestinationReached()
        {
            // Release all reservations when destination is reached
            reservationCoordinator.ReleaseAllReservations();
            
            // Forward the event to any external listeners
            OnDestinationReachedEvent?.Invoke();
        }

        private void HandlePathFailed()
        {
            // Forward the event to any external listeners
            OnPathFailedEvent?.Invoke();
        }

        // Public interface methods - for backwards compatibility with code using the original AgentController

        /// <summary>
        /// Set a new target position for the agent to move to.
        /// </summary>
        public void SetDestination(Vector2 position)
        {
            destinationSetter.SetDestination(position);
        }

        /// <summary>
        /// Set a new target position for the agent to move to (Vector3 overload).
        /// </summary>
        public void SetDestination(Vector3 position)
        {
            destinationSetter.SetDestination(position);
        }

        /// <summary>
        /// Stop following the current path.
        /// </summary>
        public void StopMovement()
        {
            pathFollower.StopMovement();
            destinationSetter.ClearDestination();
            pathRequester.ClearPath();
            reservationCoordinator.ReleaseAllReservations();
        }

        /// <summary>
        /// Get the current destination.
        /// </summary>
        public Vector2 GetDestination()
        {
            return destinationSetter.GetDestination();
        }

        /// <summary>
        /// Check if the agent has reached its destination.
        /// </summary>
        public bool HasReachedDestination()
        {
            return destinationSetter.HasReachedDestination();
        }

        /// <summary>
        /// Check if the agent is currently moving.
        /// </summary>
        public bool IsMoving()
        {
            return pathFollower.IsMoving();
        }
    }
}