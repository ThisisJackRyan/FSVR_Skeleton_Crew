using UnityEngine;
#if ENABLE_MULTIPLAYER
using UnityEngine.Networking;
#endif

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// The PullableObject is any object which can be Pulled by the character.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
#if ENABLE_MULTIPLAYER
    public class PullableObject : NetworkBehaviour
#else
    public class PullableObject : MonoBehaviour
#endif
    {
        // The amount of dampening force to apply while moving
        public float m_Dampening = 0.15f;

        // Internal variables
        private Vector3 m_PullDirection;
        private Vector3 m_BottomOffset;
        private Vector3 m_PullForce;
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

            // The bottom offset is needed so CanPull is able to determine if the object is about to hit another object.
            m_BottomOffset = -m_Transform.up * ((m_BoxCollider.size.y / 2) - 0.01f);

            // The component will be enabled when StartPull is called.
            m_Rigidbody.isKinematic = true;
            enabled = false;
        }

        /// <summary>
        /// Can the object be Pulled?
        /// </summary>
        /// <returns>True as long as another character isn't currently Pulling the object.</returns>
        public bool CanStartPull()
        {
            return enabled == false;
        }

        /// <summary>
        /// The object is going to be starting to Pull.
        /// </summary>
        /// <param name="characterTransform">The character that is going to be Pulling the object.</param>
        public void StartPull(Transform characterTransform)
        {
            // The y position should not contribute to the Pull direction.
            m_PullDirection = m_Transform.position - characterTransform.position;
            m_PullDirection.y = 0;
            m_PullDirection.Normalize();

            // Determine the closest point on the opposite side of the object. This point will be used to determine how large the object is so CanPull
            // is able to determine if the object is about to hit another object.
            var oppositePoint = m_BoxCollider.ClosestPointOnBounds(m_Transform.position + m_PullDirection * m_BoxCollider.size.magnitude);
            m_Size = (oppositePoint - m_Transform.position).magnitude + 0.1f; // Add a small buffer.

            // Start Pulling.
            m_Rigidbody.isKinematic = false;
            enabled = true;
        }

        /// <summary>
        /// Add the Pull force to the Rigidbody's velocity.
        /// </summary>
        public void FixedUpdate()
        {
#if ENABLE_MULTIPLAYER
            // Don't add any forces if not on the server. The server will move the object.
            if (!isServer) {
                return;
            }
#endif
            // Add the Pull force.
            var velocity = m_Rigidbody.velocity;
            velocity += m_PullForce;

            // Apply the dampening force to prevent the object from forever increasing in speed.
            velocity.x /= (1 + m_Dampening);
            velocity.z /= (1 + m_Dampening);

            // Set the velocity. The Pull force has been applied so can be set to zero.
            m_Rigidbody.velocity = velocity;
            m_PullForce = Vector3.zero;
        }

        /// <summary>
        /// Pull the object with the desired force.
        /// </summary>
        /// <param name="force">The force used to Pull the object.</param>
        /// <returns>Was the object Pulled?</returns>
        public bool Pull(Vector3 force)
        {
            if (CanPull(force)) {
                m_PullForce = force;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Can the object be Pulled?
        /// </summary>
        /// <param name="force">The force used to Pull the object.</param>
        /// <returns>Was the object Pulled?</returns>
        private bool CanPull(Vector3 force)
        {
            // The object cannot be Pulled if something is blocking its path.
            return !Physics.Raycast(m_Transform.position + m_BottomOffset, m_PullDirection, m_Size, LayerManager.Mask.IgnoreInvisibleLayersPlayer, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// The character is no longer Pulling the object. Disable the component.
        /// </summary>
        public void StopPull()
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
            enabled = false;
        }
    }
}