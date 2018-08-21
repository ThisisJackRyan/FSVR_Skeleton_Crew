using UnityEngine;
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
using System.Collections.Generic;
#endif

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Allows an individual magic particle to do damage to the object that was hit.
    /// </summary>
    public class MagicItemParticle : MonoBehaviour
    {
        // Internal variables
        private bool m_NormalizeDamage;

        // Component references
        private ParticleSystem m_ParticleSystem;
        private MagicItem m_MagicItem;
#if UNITY_5_3
        private ParticleCollisionEvent[] m_CollisionEvents;
#else
        private List<ParticleCollisionEvent> m_CollisionEvents = new List<ParticleCollisionEvent>();
#endif

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
#if UNITY_5_3
            m_CollisionEvents = new ParticleCollisionEvent[16];
#endif
        }

        /// <summary>
        /// Initializes the MagicItemParticle.
        /// </summary>
        /// <param name="magicItem"></param>
        /// <param name="normalizeDamage"></param>
        public void Initialize(MagicItem magicItem, bool normalizeDamage)
        {
            m_MagicItem = magicItem;
            m_NormalizeDamage = normalizeDamage;
        }

        /// <summary>
        /// The particle has collided with another object.
        /// </summary>
        /// <param name="other">The object that the particle collided with.</param>
        private void OnParticleCollision(GameObject other)
        {
#if UNITY_5_3
            // More collision event objects may need to be allocated.
            var safeLength = m_ParticleSystem.GetSafeCollisionEventSize();
            if (m_CollisionEvents.Length < safeLength) {
                m_CollisionEvents = new ParticleCollisionEvent[safeLength];
            }

            // Get all of the collision events.
            var collisionEventCount = m_ParticleSystem.GetCollisionEvents(other, m_CollisionEvents);
#else
            var collisionEventCount = m_ParticleSystem.GetCollisionEvents(other, m_CollisionEvents);
#endif
            // Call MagicItem.HitObject for each collision event.
            for (int i = 0; i < collisionEventCount; ++i) {
                m_MagicItem.HitObject(other.transform, m_CollisionEvents[i].intersection, m_CollisionEvents[i].normal, m_NormalizeDamage ? (1 / collisionEventCount) : 1);
            }
        }
    }
}