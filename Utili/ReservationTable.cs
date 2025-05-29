using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class ReservationTable
    {
        private Dictionary<Vector2Int, List<Reservation>>
            reservations = new Dictionary<Vector2Int, List<Reservation>>();

        private Dictionary<int, List<Reservation>> agentReservations = new Dictionary<int, List<Reservation>>();
        public float timeStep = 0.1f;

        public bool ReservePosition(int agentId, Vector2Int position, float timeSlot)
        {
            if (IsPositionReserved(position, timeSlot, agentId))
                return false; // Position already reserved by another agent

            if (!reservations.ContainsKey(position))
                reservations[position] = new List<Reservation>();

            if (!agentReservations.ContainsKey(agentId))
                agentReservations[agentId] = new List<Reservation>();

            var reservation = new Reservation
            {
                agentId = agentId,
                position = position,
                startTime = timeSlot,
                endTime = timeSlot + timeStep
            };

            reservations[position].Add(reservation);
            agentReservations[agentId].Add(reservation);
            return true;
        }

        public bool IsPositionReserved(Vector2Int position, float timeSlot, int excludeAgent)
        {
            if (!reservations.ContainsKey(position))
                return false;

            foreach (var reservation in reservations[position])
            {
                if (reservation.agentId != excludeAgent && reservation.IsActive(timeSlot))
                    return true;
            }

            return false;
        }

        public void ClearReservations(int agentId)
        {
            if (agentReservations.ContainsKey(agentId))
            {
                // Remove from position-based lookup
                foreach (var reservation in agentReservations[agentId])
                {
                    if (reservations.ContainsKey(reservation.position))
                    {
                        reservations[reservation.position].Remove(reservation);
                        if (reservations[reservation.position].Count == 0)
                            reservations.Remove(reservation.position);
                    }
                }

                // Clear agent's reservations
                agentReservations[agentId].Clear();
            }
        }

        public void UpdateReservations(float deltaTime)
        {
            var currentTime = Time.time;
            var expiredPositions = new List<Vector2Int>();

            foreach (var kvp in reservations)
            {
                kvp.Value.RemoveAll(r => r.IsExpired(currentTime));
                if (kvp.Value.Count == 0)
                    expiredPositions.Add(kvp.Key);
            }

            foreach (var pos in expiredPositions)
                reservations.Remove(pos);

            // Clean up agent reservations
            foreach (var kvp in agentReservations)
            {
                kvp.Value.RemoveAll(r => r.IsExpired(currentTime));
            }
        }

        public List<Reservation> GetActiveReservations()
        {
            var activeReservations = new List<Reservation>();
            var currentTime = Time.time;

            foreach (var reservationList in reservations.Values)
            {
                foreach (var reservation in reservationList)
                {
                    if (reservation.IsActive(currentTime))
                        activeReservations.Add(reservation);
                }
            }

            return activeReservations;
        }

        public List<Reservation> GetActiveReservationsInRoom(int roomId)
        {
            // This is a simplified implementation - in a real system, you'd filter by room bounds
            return GetActiveReservations();
        }

        public List<Reservation> GetAgentReservations(int agentId)
        {
            return agentReservations.ContainsKey(agentId)
                ? new List<Reservation>(agentReservations[agentId])
                : new List<Reservation>();
        }
    }
}