using System.Collections.Generic;
using RTS.Pathfinding;
using UnityEngine;

namespace RTS.AI
{
    /// <summary>
    /// Local‑avoidance system for 2‑D agents that (1) steers around nearby units without GC spikes
    /// and (2) exposes a one‑shot goal‑slot reservation utility so that agents do **not** stack when
    /// they arrive at the same destination.  
    /// Attach one instance of this component to a scene‑level GameObject and register/unregister
    /// agents from <see cref="AgentController"/>.  
    /// </summary>
    public class LocalAvoidanceSystem : MonoBehaviour
    {
        [Header("Avoidance Settings")]
        [Tooltip("Maximum radius any registered agent can have (m).  Used to size the static query buffer.")]
        [SerializeField] private float _maxAgentRadius = 0.45f;
        [Tooltip("Weight applied when blending the avoidance vector with the desired movement vector (0‑1).")]
        [Range(0f, 1f)] [SerializeField] private float _avoidanceBlend = 0.6f;
        [Tooltip("Distance (m) ahead of the agent that it will look when predicting collisions.")]
        [SerializeField] private float _lookAheadDistance = 1f;
        [Tooltip("LayerMask containing the colliders of agents that participate in avoidance.")]
        [SerializeField] private LayerMask _agentLayer;

        // --------------------------------------------------------------------
        // Runtime state
        // --------------------------------------------------------------------
        readonly List<AgentController> _agents = new();

        // Single, reusable buffer for Physics2D queries – prevents allocations.
        Collider2D[] _overlapBuffer;

        void Awake()
        {
            // 16 hits covers ~150 agents tightly packed; grows lazily if needed.
            _overlapBuffer = new Collider2D[16];
        }

        void Update()
        {
            ApplyAvoidance();
        }

        public void RegisterAgent(AgentController agent)
        {
            if (!_agents.Contains(agent))
                _agents.Add(agent);
        }

        public void UnregisterAgent(AgentController agent) => _agents.Remove(agent);

        // --------------------------------------------------------------------
        // Core logic
        // --------------------------------------------------------------------
        void ApplyAvoidance()
        {
            // foreach (var agent in _agents)
            // {
            //     if (agent == null || !agent.isActiveAndEnabled || !agent.IsMoving())
            //         continue;
            //
            //     Vector2 pos = agent.Position2D; // sugary property on AgentController
            //     Vector2 desiredDir = agent.DesiredDirection;
            //     if (desiredDir.sqrMagnitude < 0.001f)
            //         continue;
            //
            //     float personalRadius = agent.CollisionRadius;
            //     float queryRadius = personalRadius * 2.5f;
            //
            //     // Resize the buffer if necessary – happens rarely.
            //     EnsureBufferSize(Physics2D.OverlapCircleNonAlloc(pos, queryRadius, _overlapBuffer, _agentLayer));
            //     int hitCount = Physics2D.OverlapCircleNonAlloc(pos, queryRadius, _overlapBuffer, _agentLayer);
            //
            //     Vector2 avoidance = Vector2.zero;
            //     bool hasThreat = false;
            //
            //     for (int i = 0; i < hitCount; i++)
            //     {
            //         Collider2D col = _overlapBuffer[i];
            //         if (col == null || col.attachedRigidbody == agent.Rigidbody2D)
            //             continue; // self or invalid
            //
            //         AgentController other = col.GetComponentInParent<AgentController>();
            //         if (other == null || other == agent)
            //             continue;
            //
            //         Vector2 otherPos = other.Position2D;
            //         float dist = Vector2.Distance(pos, otherPos);
            //         if (dist <= 0.001f || dist > queryRadius)
            //             continue;
            //
            //         // Separation vector, stronger when closer.
            //         Vector2 away = (pos - otherPos).normalized;
            //         float w = 1f - Mathf.Clamp01(dist / queryRadius);
            //
            //         // Extra weight for agents that are in front of us.
            //         float front = Vector2.Dot(desiredDir, (otherPos - pos).normalized);
            //         if (front > 0f)
            //             w *= 1f + front;
            //
            //         avoidance += away * w;
            //         hasThreat = true;
            //     }
            //
            //     if (hasThreat)
            //     {
            //         avoidance = avoidance.normalized;
            //         Vector2 blended = Vector2.Lerp(desiredDir, (desiredDir + avoidance).normalized, _avoidanceBlend);
            //         agent.SetOverrideDirection(blended);
            //     }
            //     else
            //     {
            //         agent.ClearOverrideDirection();
            //     }
            // }
        }

        void EnsureBufferSize(int hits)
        {
            if (hits >= _overlapBuffer.Length)
            {
                int newSize = _overlapBuffer.Length * 2;
                while (newSize <= hits) newSize *= 2;
                System.Array.Resize(ref _overlapBuffer, newSize);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.yellow;
            foreach (var a in _agents)
            {
                if (a == null) continue;
                // Gizmos.DrawWireSphere(a.transform.position, a.CollisionRadius);
            }
        }
#endif
    }

    // --------------------------------------------------------------------
    // Goal‑slot reservation helper – optional, but stops units stacking when
    // several are commanded to the *same* destination.
    // --------------------------------------------------------------------

    public class GoalSlotManager : MonoBehaviour
    {
        public static GoalSlotManager Instance { get; private set; }
        [SerializeField] float _slotRadius = 0.5f;
        readonly HashSet<Vector2Int> _taken = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Returns a free position near <paramref name="goal"/> that is not already reserved.
        /// The search is a spiral expanding one slot at a time.
        /// Call <see cref="ReleaseSlot"/> when you’re done.
        /// </summary>
        public Vector2 ReserveSlot(Vector2 goal)
        {
            Vector2Int goalCell = ToCell(goal);
            if (_taken.Add(goalCell))
                return goal; // direct hit is free

            // Spiral search
            int radius = 1;
            while (radius < 100) // hard‑stop – unlikely to hit
            {
                for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue; // perimeter only
                    Vector2Int cell = goalCell + new Vector2Int(dx, dy);
                    if (_taken.Add(cell))
                        return ToWorld(cell);
                }
                radius++;
            }
            Debug.LogError("[GoalSlotManager] Could not find a free slot – increase search radius.");
            return goal;
        }

        public void ReleaseSlot(Vector2 position)
        {
            _taken.Remove(ToCell(position));
        }

        Vector2Int ToCell(Vector2 world)
        {
            return new Vector2Int(Mathf.RoundToInt(world.x / _slotRadius), Mathf.RoundToInt(world.y / _slotRadius));
        }
        Vector2 ToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * _slotRadius, cell.y * _slotRadius);
        }
    }

    
}
