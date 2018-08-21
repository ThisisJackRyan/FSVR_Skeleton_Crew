using UnityEngine;
#if ENABLE_MULTIPLAYER
using UnityEngine.Networking;
#endif

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// The MoveableObject is any object which can be pushed by the character.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
#if ENABLE_MULTIPLAYER
    public class MoveableObject : NetworkBehaviour
#else
    public class MoveableObject : MonoBehaviour
#endif
    {
        // The amount of dampening force to apply while moving
        public float m_Dampening = 0.15f;

        // Internal variables
        private Vector3 m_MoveDirection;
        private Vector3 m_BottomOffset;
        private Vector3 m_MoveForce;
        private float m_Size;

        // Component references
        private Transform m_Transform;
        private Rigidbody m_Rigidbody;
        private BoxCollider m_BoxCollider;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_Transform = transform;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_BoxCollider = GetComponent<BoxCollider>();

            // The bottom offset is needed so CanMove is able to determine if the object is about to hit another object.
            m_BottomOffset = -m_Transform.up * ((m_BoxCollider.size.y / 2) - 0.01f);

            // The component will be enabled when StartMove is called.
            m_Rigidbody.isKinematic = true;
            enabled = false;
        }

        /// <summary>
        /// Can the object be moved?
        /// </summary>
        /// <returns>True as long as another character isn't currently moving the object.</returns>
        public bool CanStartMove()
        {
            return enabled == false;
        }

        /// <summary>
        /// The object is going to be starting to move.
        /// </summary>
        /// <param name="characterTransform">The character that is going to be moving the object.</param>
        public void StartMove(Transform characterTransform)
        {
            // The y position should not contribute to the move direction.
            m_MoveDirection = m_Transform.position - characterTransform.position;
            m_MoveDirection.y = 0;
            m_MoveDirection.Normalize();

            // Determine the closest point on the opposite side of the object. This point will be used to determine how large the object is so CanMove
            // is able to determine if the object is about to hit another object.
            var oppositePoint = m_BoxCollider.ClosestPointOnBounds(m_Transform.position + m_MoveDirection * m_BoxCollider.size.magnitude);
            m_Size = (oppositePoint - m_Transform.position).magnitude + 0.1f; // Add a small buffer.

            // Start moving.
            m_Rigidbody.isKinematic = false;
            enabled = true;
        }

        /// <summary>
        /// Add the move force to the Rigidbody's velocity.
        /// </summary>
        public void FixedUpdate()
        {
#if ENABLE_MULTIPLAYER
            // Don't add any forces if not on the server. The server will move the object.
            if (!isServer) {
                return;
            }
#endif
            // Add the move force.
            var velocity = m_Rigidbody.velocity;
            velocity += m_MoveForce;

            // Apply the dampening force to prevent the object from forever increasing in speed.
            velocity.x /= (1 + m_Dampening);
            velocity.z /= (1 + m_Dampening);

            // Set the velocity. The move force has been applied so can be set to zero.
            m_Rigidbody.velocity = velocity;
            m_MoveForce = Vector3.zero;
        }

        /// <summary>
        /// Move the object with the desired force.
        /// </summary>
        /// <param name="force">The force used to move the object.</param>
        /// <returns>Was the object moved?</returns>
        public bool Move(Vector3 force)
        {
            if (CanMove(force)) {
                m_MoveForce = force;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Can the object be moved?
        /// </summary>
        /// <param name="force">The force used to move the object.</param>
        /// <returns>Was the object moved?</returns>
        private bool CanMove(Vector3 force)
        {
            // The object cannot be moved if something is blocking its path.
            return !Physics.Raycast(m_Transform.position + m_BottomOffset, m_MoveDirection, m_Size, LayerManager.Mask.IgnoreInvisibleLayersPlayer, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// The character is no longer moving the object. Disable the component.
        /// </summary>
        public void StopMove()
        {
            // The Rigidbody may be null if the game stops while the ability is active.
            if (m_Rigidbody == null) {
                return;
            }
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
            enabled = false;
        }
    }
}