using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class ReservationTable
    {
        private readonly Dictionary<Vector2Int, List<Reservation>> reservations;
        private readonly float timeStep;
        private float currentTime;

        public ReservationTable(float step = 0.5f)
        {
            reservations = new Dictionary<Vector2Int, List<Reservation>>();
            timeStep = step;
            currentTime = 0;
        }

        public bool ReservePosition(int agentId, Vector2Int position, float timeSlot)
        {
            if (!reservations.ContainsKey(position))
                reservations[position] = new List<Reservation>();

            // Check for conflicts
            foreach (var reservation in reservations[position])
                if (reservation.AgentId != agentId &&
                    reservation.StartTime <= timeSlot && reservation.EndTime >= timeSlot)
                    return false;

            reservations[position].Add(new Reservation(agentId, position, timeSlot, timeSlot + timeStep));
            return true;
        }

        public bool IsPositionReserved(Vector2Int position, float timeSlot, int excludeAgent)
        {
            if (!reservations.ContainsKey(position))
                return false;

            return reservations[position].Any(r =>
                r.AgentId != excludeAgent && r.IsActive(timeSlot));
        }

        public void ClearReservations(int agentId)
        {
            foreach (var posList in reservations.Values) posList.RemoveAll(r => r.AgentId == agentId);
        }

        public void UpdateReservations(float deltaTime)
        {
            currentTime += deltaTime;

            // Remove expired reservations
            foreach (var posList in reservations.Values) posList.RemoveAll(r => r.IsExpired(currentTime));
        }

        public List<Reservation> GetActiveReservations()
        {
            var active = new List<Reservation>();
            foreach (var posList in reservations.Values) active.AddRange(posList.Where(r => r.IsActive(currentTime)));
            return active;
        }
    }
}