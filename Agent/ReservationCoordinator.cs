using UnityEngine;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Handles tile reservations for navigation.
    /// </summary>
    public class ReservationCoordinator : MonoBehaviour
    {
        [SerializeField] private AStarNavAgent navAgent;
        [SerializeField] private ReservationTable reservationTable;
        [SerializeField] private int lookAheadSteps = 5;

        private List<int> currentPathIndices;
        private int currentWaypointIndex;

        private void Awake()
        {
            if (navAgent == null) navAgent = GetComponent<AStarNavAgent>();
            if (reservationTable == null) reservationTable = FindObjectOfType<ReservationTable>();
            currentPathIndices = new List<int>();
        }

        private void OnDestroy()
        {
            // Release all reservations
            if (reservationTable != null)
            {
                ReleaseAllReservations();
            }
        }

        /// <summary>
        /// Set the current path indices and reserve the upcoming path segments.
        /// </summary>
        public void SetPath(List<int> pathIndices, int waypointIndex)
        {
            // Release any existing reservations first
            ReleaseAllReservations();
            
            currentPathIndices = pathIndices;
            currentWaypointIndex = waypointIndex;

            // Reserve the path
            ReservePath();
        }

        /// <summary>
        /// Update the current waypoint index and advance reservations.
        /// </summary>
        public void UpdateWaypointIndex(int newWaypointIndex)
        {
            currentWaypointIndex = newWaypointIndex;
            AdvanceReservations();
        }

        // Reserve the path in the reservation table
        private void ReservePath()
        {
            if (reservationTable == null || currentPathIndices == null || currentPathIndices.Count == 0)
                return;

            // Reserve the next few steps
            int startIdx = currentWaypointIndex;
            int endIdx = Mathf.Min(startIdx + lookAheadSteps, currentPathIndices.Count);

            if (endIdx > startIdx)
            {
                List<int> pathSegment = new List<int>();
                for (int i = startIdx; i < endIdx; i++)
                {
                    pathSegment.Add(currentPathIndices[i]);
                }

                reservationTable.ReserveSequence(pathSegment, 0, navAgent);
            }
        }

        // Advance reservations when moving to the next waypoint
        private void AdvanceReservations()
        {
            if (reservationTable == null || currentPathIndices == null || currentPathIndices.Count == 0)
                return;

            // Release the previous reservation
            reservationTable.AdvanceTime();

            // Reserve the next step in sequence
            int nextLookAheadIndex = currentWaypointIndex + lookAheadSteps - 1;
            if (nextLookAheadIndex < currentPathIndices.Count)
            {
                int nextIndex = currentPathIndices[nextLookAheadIndex];
                reservationTable.Reserve(nextIndex, lookAheadSteps - 1, navAgent);
            }
        }

        /// <summary>
        /// Release all reservations.
        /// </summary>
        public void ReleaseAllReservations()
        {
            if (reservationTable != null)
            {
                reservationTable.ReleaseAll(navAgent);
            }
        }

        /// <summary>
        /// Clear path data.
        /// </summary>
        public void ClearPath()
        {
            currentPathIndices.Clear();
            ReleaseAllReservations();
        }
    }
}   