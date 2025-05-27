using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class ReservationTable
    {
        private float currentTime;
        private readonly Dictionary<Vector2Int, List<Reservation>> reservations = new();
        private readonly float timeStep = 0.1f;

        public bool ReservePosition(int agentId, Vector2Int position, float timeSlot)
        {
            if (!reservations.ContainsKey(position))
                reservations[position] = new List<Reservation>();

            // Check if position is already reserved
            foreach (var reservation in reservations[position])
                if (reservation.IsActive(timeSlot) && reservation.AgentId != agentId)
                    return false;

            // Add reservation
            reservations[position].Add(new Reservation
            {
                AgentId = agentId,
                Position = position,
                StartTime = timeSlot,
                EndTime = timeSlot + timeStep
            });

            return true;
        }

        public bool IsPositionReserved(Vector2Int position, float timeSlot, int excludeAgent)
        {
            if (!reservations.ContainsKey(position))
                return false;

            foreach (var reservation in reservations[position])
                if (reservation.AgentId != excludeAgent && reservation.IsActive(timeSlot))
                    return true;

            return false;
        }

        public void ClearReservations(int agentId)
        {
            foreach (var positionReservations in reservations.Values)
                positionReservations.RemoveAll(r => r.AgentId == agentId);
        }

        public void UpdateReservations(float deltaTime)
        {
            currentTime += deltaTime;

            // Clean up expired reservations
            foreach (var positionReservations in reservations.Values)
                positionReservations.RemoveAll(r => r.IsExpired(currentTime));
        }
    }
}