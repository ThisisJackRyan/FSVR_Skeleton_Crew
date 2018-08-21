using UnityEngine;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Allows multiple hitboxes to be used on a single MeleeWeapon or MeleeWeaponExtension.
    /// </summary>
    public class MeleeWeaponHitbox : MonoBehaviour
    {
        // Internal fields
        private IHitboxItem m_Owner;
        private bool m_IgnoredColliders;

        // Component references
        [System.NonSerialized] private GameObject m_GameObject;
        private Rigidbody m_Rigidbody;

        // Exposed properties
        public IHitboxItem Owner { set { m_Owner = value; } }

        /// <summary>
        /// Initialize the default values and cache the component references.
        /// </summary>
        private void Awake()
        {
            m_GameObject = gameObject;
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null) {
                Debug.LogError("Error: The MeleeWeaponHitbox must have a Rigidbody attached.");
            }
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        /// <summary>
        /// Activates or deactivates the hitbox.
        /// </summary>
        /// <param name="active">Should the hitbox be activated?</param>
        public void SetActive(bool active)
        {
            m_GameObject.SetActive(active);
            m_Rigidbody.isKinematic = !active;

            if (active && !m_IgnoredColliders) {
                // Perminately ignore the character colliders.
                var colliders = GetComponents<Collider>();
                var characterColliders = GetComponentInParent<RigidbodyCharacterController>().GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; ++i) {
                    for (int j = 0; j < characterColliders.Length; ++j) {
                        if (characterColliders[j].enabled) {
                            Physics.IgnoreCollision(colliders[i], characterColliders[j], true);
                        }
                    }
                }

                m_IgnoredColliders = true;
            }
        }

        /// <summary>
        /// The collider has collided with another object. Notify the ower.
        /// </summary>
        /// <param name="collision">The object that collided with the hitbox.</param>
        private void OnCollisionEnter(Collision collision)
        {
            m_Owner.HitboxCollision(collision);
        }
    }
}