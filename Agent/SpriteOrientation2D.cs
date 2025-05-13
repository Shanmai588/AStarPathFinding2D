using UnityEngine;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Handles sprite orientation for 2D movement.
    /// </summary>
    public class SpriteOrientation2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipSpriteOnDirectionChange = true;
        [SerializeField] private bool rotateToFaceDirection = false;
        [SerializeField] private float rotationOffset = -90f; // Adjust based on your sprite's forward direction

        private PathFollower2D pathFollower;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            pathFollower = GetComponent<PathFollower2D>();
        }

        private void Update()
        {
            if (pathFollower != null && pathFollower.IsMoving())
            {
                UpdateOrientation(pathFollower.GetDesiredDirection());
            }
        }

        /// <summary>
        /// Update the sprite orientation based on movement direction.
        /// </summary>
        public void UpdateOrientation(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            if (flipSpriteOnDirectionChange && spriteRenderer != null)
            {
                // Flip sprite based on horizontal direction
                spriteRenderer.flipX = direction.x < 0;
            }

            if (rotateToFaceDirection)
            {
                // Calculate angle in degrees from direction vector
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // Rotate to face direction (adjusting for sprite orientation)
                transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
            }
        }

        /// <summary>
        /// Force update orientation with specific direction.
        /// </summary>
        public void ForceUpdateOrientation(Vector2 direction)
        {
            UpdateOrientation(direction);
        }
    }
}