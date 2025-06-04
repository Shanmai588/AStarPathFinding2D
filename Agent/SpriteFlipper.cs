// SpriteFlipper.cs
using UnityEngine;

namespace RTS.Pathfinding // Or your preferred namespace
{
    [RequireComponent(typeof(Motor))] // Ensures Motor is also on this GameObject
    public class SpriteFlipper : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Motor motor;
        private bool facingRight = true; // Assume default sprite faces right

        // A small threshold to prevent flipping from very minor horizontal movements
        private const float flipThreshold = 0.01f;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            motor = GetComponent<Motor>();

            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteFlipper: SpriteRenderer component not found!", this);
                enabled = false; // Disable this component if no SpriteRenderer
                return;
            }
            if (motor == null)
            {
                Debug.LogError("SpriteFlipper: Motor component not found!", this);
                enabled = false; // Disable this component if no Motor
                return;
            }

            // Optional: Set initial flip based on an initial motor direction if any,
            // or assume a default. For now, we rely on the 'facingRight' default.
        }

        private void Update()
        {
            if (!motor.IsMoving || motor.CurrentMovementDirection == Vector2.zero)
            {
                // Not moving or direction is zero, do nothing with flipping
                // The sprite will retain its last facing direction
                return;
            }

            float horizontalMovement = motor.CurrentMovementDirection.x;

            if (horizontalMovement > flipThreshold && !facingRight)
            {
                Flip();
            }
            else if (horizontalMovement < -flipThreshold && facingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            facingRight = !facingRight;
            spriteRenderer.flipX = !facingRight; // If facingRight is true, flipX is false.
        }

        /// <summary>
        /// Sets the initial facing direction of the sprite.
        /// Call this if your sprite's default graphic doesn't face right.
        /// </summary>
        /// <param name="isFacingRight">True if the sprite's default image faces right.</param>
        public void SetInitialFacingDirection(bool isFacingRight)
        {
            facingRight = isFacingRight;
            // Apply initial flip state based on this new understanding of "facingRight"
            // If it's "supposed" to be facing right, flipX should be false.
            // If it's "supposed" to be facing left (meaning facingRight is false), flipX should be true.
            spriteRenderer.flipX = !facingRight;
        }
    }
}