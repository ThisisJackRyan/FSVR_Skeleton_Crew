using UnityEngine;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// An item which can cast a magic spell when used.
    /// </summary>
    public class MagicItem : Item, IUseableItem
    {
        /// <summary>
        /// Specifies how often the magic is casted
        /// </summary>
        public enum CastMode
        {
            Single, // Cast the magic once when Used is called
            Continuous // Cast the magic continuously until the end
        }

        /// <summary>
        /// Specifies the shape of the magic cast
        /// </summary>
        public enum CastShape
        {
            Linear, // Cast the magic in the forward direction
            Spherical, // Cast the magic 360 degrees around the character
            Particle // Use the particle collision event to determine if the magic item hit an object
        }
        
        [Tooltip("The state while using the item")]
        [SerializeField] protected AnimatorItemCollectionData m_UseStates = new AnimatorItemCollectionData("Use", "Use", 0.2f, true);
        
        [Tooltip("The input name mapped to use the item")]
        [SerializeField] protected string m_UseInputName = "Fire1";
        [Tooltip("Can the item be used in the air?")]
        [SerializeField] protected bool m_CanUseInAir = true;
        [Tooltip("Specifies how often the magic is casted")]
        [SerializeField] protected CastMode m_CastMode = CastMode.Continuous;
        [Tooltip("Specifies the shape of the cast")]
        [SerializeField] protected CastShape m_CastShape;
        [Tooltip("The number of casts per second")]
        [SerializeField] protected float m_CastRate = 2;
        [Tooltip("The point at which to do the actual cast")]
        [SerializeField] protected Transform m_CastPoint;
        [Tooltip("The distance of the cast. Only used if the CastShape is linear")]
        [SerializeField] protected float m_CastDistance = 1;
        [Tooltip("The radius of the cast")]
        [SerializeField] protected float m_CastRadius = 1;
        [Tooltip("The amount of ConsumableItem  to use for each item use")]
        [SerializeField] protected int m_CastAmount = 1;
        [Tooltip("Specifies the maximum number of colliders that the cast can hit")]
        [SerializeField] protected int m_MaxHitColliders = 20;
        [Tooltip("The layers that the cast can hit")]
        [SerializeField] protected LayerMask m_TargetLayer;
        [Tooltip("Should the magic wait for the OnAnimatorItemEndUse to return to a non-use state?")]
        [SerializeField] protected bool m_WaitForEndUseEvent;
        [Tooltip("Can the magic be stopped before the used method is called?")]
        [SerializeField] protected bool m_CanStopBeforeUse = true;
        [Tooltip("Minimum amount of time that the continuous item can be used")]
        [SerializeField] protected float m_ContinuousMinUseDuration = 1;
        
        [Tooltip("The speed at which to regenerate the ammo")]
        [SerializeField] protected float m_RegenerateRate;
        [Tooltip("The amount of ammo to add each regenerative tick. RegenerativeRate must be greater than 0")]
        [SerializeField] protected int m_RegenerateAmount;

        [Tooltip("Optionally specify any particles that should play when the magic is casted")]
        [SerializeField] protected ParticleSystem m_CastParticles;
        [Tooltip("Optionally specify a sound that should randomly play when the magic is casted")]
        [SerializeField] protected AudioClip[] m_CastSound;
        [Tooltip("If Cast Sound is specified, play the sound after the specified delay")]
        [SerializeField] protected float m_CastSoundDelay;
        
        [Tooltip("Optionally specify an event to send to the object hit by the cast")]
        [SerializeField] protected string m_DamageEvent;
        [Tooltip("The amount of damage done to the object hit")]
        [SerializeField] protected float m_DamageAmount = 10;
        [Tooltip("If the cast hits multiple objects should the damage amount be distributed evenly across all objects?")]
        [SerializeField] protected bool m_NormalizeDamage;
        [Tooltip("How much force is applied to the object hit")]
        [SerializeField] protected float m_ImpactForce = 5;
        [Tooltip("Optionally specify a default impact sound that should play at the point of the object hit. This is only used if no per-object sound is setup in the ObjectManager")]
        [SerializeField] protected AudioClip m_DefaultImpactSound;
        [Tooltip("Optionally specify any default sparks that should appear on top of the object hit. This is only used if no per-object spark is setup in the ObjectManager")]
        [SerializeField] protected GameObject m_DefaultSpark;

        // SharedFields
        private SharedMethod<Vector3, bool, Vector3> m_TargetLookDirectionLookPoint = null;
        private Collider[] m_HitColliders;

        // Exposed properties for the Item Builder
        public AnimatorItemCollectionData UseStates { get { return m_UseStates; } }

        // Internal variables
        private float m_UseDelay;
        private float m_LastCastTime;
        private bool m_InUse;
        private bool m_Used;
        private bool m_StopContinuousUse;
        private ScheduledEvent m_UseEvent;
        private float m_RegenerateDelay;
        private ScheduledEvent m_RegenerateEvent;

        // Component references
        private AudioSource m_AudioSource;
        private MagicItemParticle m_MagicItemParticle;
        private Transform m_CharacterTransform;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            m_AudioSource = GetComponent<AudioSource>();
            m_HitColliders = new Collider[m_MaxHitColliders];

            m_UseDelay = 1.0f / m_CastRate;
            m_LastCastTime = -m_CastRate;
            if (m_RegenerateRate > 0) {
                m_RegenerateDelay = 1.0f / m_RegenerateRate;
            }
            if (m_CastParticles != null) {
                m_MagicItemParticle = m_CastParticles.GetComponent<MagicItemParticle>();
                if (m_MagicItemParticle != null) {
                    m_MagicItemParticle.Initialize(this, m_NormalizeDamage);
                }
            }
        }

        /// <summary>
        /// Prepare the item for use.
        /// </summary>
        protected override void ItemActivated()
        {
            base.ItemActivated();

            EventHandler.RegisterEvent<Transform, Vector3, Vector3>(gameObject, "OnItemAddCastHitEffects", AddCastHitEffects);
            EventHandler.RegisterEvent(gameObject, "OnItemAddCastEffects", AddCastEffects);
            if (m_WaitForEndUseEvent) {
                EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
            }
        }

        /// <summary>
        /// The item is no longer equipped.
        /// </summary>
        protected override void ItemDeactivated()
        {
            // The effect may still be playing if the character died while the animation is playing.
            EndUse();

            base.ItemDeactivated();

            // The animation states should begin fresh.
            m_UseStates.ResetNextState();

            EventHandler.UnregisterEvent<Transform, Vector3, Vector3>(gameObject, "OnItemAddCastHitEffects", AddCastHitEffects);
            EventHandler.UnregisterEvent(gameObject, "OnItemAddCastEffects", AddCastEffects);
            // The character may be null if Init hasn't been called yet.
            if (m_Character != null) {
                EventHandler.UnregisterEvent<bool>(m_Character, "OnControllerAim", OnAim);
                if (m_WaitForEndUseEvent) {
                    EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
                }
            }
        }

        /// <summary>
        /// Initializes the item after it has been added to the Inventory.
        /// </summary>
        /// <param name="inventory">The parent character's inventory.</param>
        public override void Init(Inventory inventory)
        {
            base.Init(inventory);

            m_CharacterTransform = m_Character.transform;

            // Initialize the animation states.
            m_UseStates.Initialize(m_ItemType);

            EventHandler.RegisterEvent<Item, bool, bool>(m_Character, "OnInventoryConsumableItemCountChange", ConsumableItemCountChange);
        }

        /// <summary>
        /// Returns the input name for the item to be used.
        /// </summary>
        /// <param name="dualWield">Is the dual wield mapping being retrieved?</returns>
        /// <returns>The input name for the item to be used.</returns>
        public string GetUseInputName(bool dualWield)
        {
            return  m_UseInputName;
        }

        /// <summary>
        /// Returns the destination state for the given layer.
        /// </summary>
        /// <param name="priority">Specifies the item animation priority to retrieve. High priority animations get tested before lower priority animations.</param>
        /// <param name="layer">The Animator layer index.</param>
        /// <returns>The state that the Animator should be in for the given layer. A null value indicates no change.</returns>
        public override AnimatorItemStateData GetDestinationState(ItemAnimationPriority priority, int layer)
        {
            var state = base.GetDestinationState(priority, layer);
            if (state != null) {
                return state;
            }

            // Item use is a high priority item.
            if (priority == ItemAnimationPriority.High) {
                if (InUse()) {
                    state = m_UseStates.GetState(layer, m_Controller.Moving);
                    if (state != null) {
                        return state;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Can the item be used?
        /// </summary>
        /// <returns>True if the item can be used.</returns>
        public bool CanUse()
        {
            if (!m_CanUseInAir && !m_Controller.Grounded) {
                return false;
            }
            return !m_InUse;
        }

        /// <summary>
        /// Try to cast the magic. The item may not be able be casted if the last cast time was too recent.
        /// <returns>True if the item was used.</returns>
        /// </summary>
        public bool TryUse()
        {
            if (!m_InUse && m_LastCastTime + m_UseDelay < Time.time) {
                if (m_Inventory.GetItemCount(m_ItemType) >= m_CastAmount) {
                    m_LastCastTime = Time.time;
                    m_InUse = true;
                    // Do not regenerate any more ammo after starting to use the item.
                    if (m_RegenerateEvent != null) {
                        Scheduler.Cancel(ref m_RegenerateEvent);
                    }
                    EventHandler.ExecuteEvent(m_Character, "OnItemUse");
                    return true;
                } else {
                    EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
                }
            }
            return false;
        }

        /// <summary>
        /// The magic has been casted. Damage any objects hit.
        /// </summary>
        public void Used()
        {
#if ENABLE_MULTIPLAYER
            // The server will control the raycast logic.
            if (!m_IsServer.Invoke()) {
                return;
            }
#endif

            if (!m_Used) {
                m_Used = true;
                CastMagic();
            }
        }

        /// <summary>
        /// Do the actual cast.
        /// </summary>
        protected virtual void CastMagic()
        {
            // Decrement the amount of ammo from the inventory.
            m_Inventory.UseItem(m_ItemType, m_CastAmount);

            // Temporarily set the character to a layer not being attacked. This will prevent the spherecast from hitting the character.
            var prevLayer = m_Character.layer;
            m_Character.layer = LayerManager.IgnoreRaycast;

            var castPosition = CastPosition();
            if (m_CastShape == CastShape.Linear) {
                var castDirection = CastDirection();
                // Allow the cast to hit multiple objects with SphereCastAll.
                var hit = Physics.SphereCastAll(castPosition - castDirection * m_CastRadius, m_CastRadius, castDirection, m_CastDistance + m_CastRadius, m_TargetLayer.value, QueryTriggerInteraction.Ignore);
                var damageStrength = m_NormalizeDamage ? (1 / hit.Length) : 1;
                for (int i = 0; i < hit.Length; ++i) {
                    HitObject(hit[i].transform, hit[i].point, hit[i].normal, damageStrength);
                }
            } else if (m_CastShape == CastShape.Linear) {
                // If the CastShape is a sphere then cast a sphere at the cast point.
                var hitCount = Physics.OverlapSphereNonAlloc(castPosition, m_CastRadius, m_HitColliders, m_TargetLayer.value, QueryTriggerInteraction.Ignore);
                var damageStrength = m_NormalizeDamage ? (1 / hitCount) : 1;
                for (int i = 0; i < hitCount; ++i) {
                    var closestPoint = m_HitColliders[i].ClosestPointOnBounds(castPosition);
                    HitObject(m_HitColliders[i].transform, closestPoint, (castPosition - closestPoint).normalized, damageStrength);
                }
            } // If the CastShape is particle then the MagicItemParticle component will call HitObject.

            m_Character.layer = prevLayer;

            // Add any use starting effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
            m_NetworkMonitor.ExecuteItemEvent(m_ItemType.ID, "OnItemAddCastEffects");
#else
            AddCastEffects();
#endif

            // Determine if the cast should end or if it should continue again.
            if (m_CastMode == CastMode.Continuous) {
                if (!m_WaitForEndUseEvent && (m_StopContinuousUse && m_LastCastTime + m_ContinuousMinUseDuration < Time.time)) {
                    EndUse();
                } else {
                    m_UseEvent = Scheduler.Schedule(m_UseDelay, CastMagic);
                }
            } else if (!m_WaitForEndUseEvent) {
                EndUse();
            }
        }

        /// <summary>
        /// The magic item has been used, add any effects.
        /// </summary>
        private void AddCastEffects()
        {
            // Play any particle effects.
            if (m_CastParticles) {
                m_CastParticles.Play(true);
            }

            // Play a use sound.
            if (m_CastSound != null && m_CastSound.Length > 0) {
                m_AudioSource.clip = m_CastSound[Random.Range(0, m_CastSound.Length)];
                m_AudioSource.loop = m_CastMode == CastMode.Continuous;
                if (m_CastSoundDelay > 0) {
                    m_AudioSource.PlayDelayed(m_CastSoundDelay);
                } else {
                    m_AudioSource.Play();
                }
            }
        }

        /// <summary>
        /// A cast has hit an object. Send any custom effects, damage the object, and play any effects.
        /// </summary>
        /// <param name="hitTransform">The Transform of the object hit.</param>
        /// <param name="hitPoint">The position of the hit point.</param>
        /// <param name="hitNormal">The normal of the hit point.</param>
        /// <param name="damageStrength">Specifies the strength of the damage amount (0 - 1).</param>
        public void HitObject(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal, float damageStrength)
        {
            // Execute any custom events.
            if (!string.IsNullOrEmpty(m_DamageEvent)) {
                EventHandler.ExecuteEvent(hitTransform.gameObject, m_DamageEvent, m_DamageAmount * damageStrength, hitPoint, hitNormal * -m_ImpactForce, m_Character);
            }
            Health hitHealth;
            Rigidbody hitRigidbody;
            // If the Health component exists it will apply a force to the rigidbody in addition to deducting the health. Otherwise just apply the force to the rigidbody. 
            if ((hitHealth = hitTransform.GetComponentInParent<Health>()) != null) {
                hitHealth.Damage(m_DamageAmount * damageStrength, hitPoint, hitNormal * -m_ImpactForce, m_Character, hitTransform.gameObject);
            } else if (m_ImpactForce > 0 && (hitRigidbody = Utility.GetComponentForType<Rigidbody>(hitTransform.gameObject)) != null && !hitRigidbody.isKinematic) {
                hitRigidbody.AddForceAtPosition(hitNormal * -m_ImpactForce, hitPoint);
            }

            // Add any cast effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
            m_NetworkMonitor.ExecuteItemEvent(m_ItemType.ID, "OnItemAddCastHitEffects", hitTransform.gameObject, hitPoint, hitNormal);
#else
            AddCastHitEffects(hitTransform, hitPoint, hitNormal);
#endif
        }

        /// <summary>
        /// The cast item hit an object, add any effects.
        /// </summary>
        /// <param name="hitTransform">The transform that was hit.</param>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="hitNormal">The normal of the transform at the hit point.</param>
        private void AddCastHitEffects(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal)
        {
            Object retrievedObject = null;
            // Spawn a spark particle effect at the hit point.
            GameObject spark;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ItemType, ObjectManager.ObjectCategory.Spark, ref retrievedObject)) {
                spark = retrievedObject as GameObject;
            } else {
                spark = m_DefaultSpark;
            }
            if (spark != null) {
                ObjectPool.Instantiate(spark, hitPoint, spark.transform.rotation * Quaternion.LookRotation(hitNormal));
            }

            // Play a sound at the hit point.
            AudioClip audioClip;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ItemType, ObjectManager.ObjectCategory.Audio, ref retrievedObject)) {
                audioClip = retrievedObject as AudioClip;
            } else {
                audioClip = m_DefaultImpactSound;
            }
            if (audioClip != null) {
                AudioSource.PlayClipAtPoint(audioClip, hitPoint);
            }
        }

        /// <summary>
        /// Ends the item use.
        /// </summary>
        private void EndUse()
        {
            if (!InUse()) {
                return;
            }
            if (m_UseEvent != null) {
                Scheduler.Cancel(ref m_UseEvent);
            }
            if (m_CastParticles) {
                m_CastParticles.Stop();
            }
            if (m_CastSound != null && m_CastSound.Length > 0 && m_CastMode == CastMode.Continuous) {
                m_AudioSource.Stop();
            }
            if (m_RegenerateDelay != 0) {
                m_RegenerateEvent = Scheduler.Schedule(m_RegenerateDelay, RegenerateAmmo);
            }
            m_InUse = false;
            m_Used = false;
            m_StopContinuousUse = false;
            m_AimStates.NextState();
            m_UseStates.NextState();
            EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
        }

        /// <summary>
        /// Returns the position to start the cast from.
        /// </summary>
        /// <returns>The position to start the cast from.</returns>
        private Vector3 CastPosition()
        {
            if (m_CastPoint == null) {
                var position = m_CharacterTransform.position;
                position.y = m_Transform.position.y;
                return position;
            }
            return m_CastPoint.position;
        }

        /// <summary>
        /// Determines the direction to cast the magic based on the camera's look position.
        /// </summary>
        /// <returns>The direction to cast the item.</returns>
        private Vector3 CastDirection()
        {
            // If m_TargetLookDirectionLookPoint is null then use the forward direction. It may be null if the AI agent doesn't have the AIAgent component attached.
            if (m_TargetLookDirectionLookPoint == null) {
                return m_CharacterTransform.forward;
            } else {
                var direction = m_TargetLookDirectionLookPoint.Invoke(CastPosition(), true);
                // Don't let the character cast in the opposite direction of the weapon.
                if (Vector3.Dot(m_CharacterTransform.forward, direction) < 0) {
                    direction = m_CharacterTransform.forward;
                }
                return direction;
            }
        }

        /// <summary>
        /// Is the magic item currently being used?
        /// </summary>
        /// <returns>True if the item is in use.</returns>
        public bool InUse()
        {
            return m_InUse;
        }

        /// <summary>
        /// Stops the use of a contiuous magic cast.
        /// </summary>
        public void TryStopUse()
        {
            if (m_CanStopBeforeUse || ((m_CastMode == CastMode.Single && m_Used) || m_CastMode == CastMode.Continuous)) {
                // The Used method will stop the Continuous Mode.
                if (!m_CanStopBeforeUse && m_CastMode == CastMode.Continuous) {
                    m_StopContinuousUse = true;
                } else {
                    EndUse();
                }
            }
        }

        /// <summary>
        /// Event notification that the Inventory has added or removed consumable items. Determine if the item needs to be reloaded.
        /// </summary>
        /// <param name="item">The item whose consumable ammo has changed.</param>
        /// <param name="added">True if the consumable items were added.</param>
        /// <param name="immediateChange">True if the consumable item count should be changed immediately. This will be true when the player initially spawns.</param>
        private void ConsumableItemCountChange(Item item, bool added, bool immediateChange)
        {
            // DualWield items require the GameObject to be active. The DualWielded item may never be picked up so it doesn't need to take ammo from the PrimaryItem.
            if (added && (item.ItemType.Equals(m_ItemType) || (gameObject.activeSelf && m_ItemType is DualWieldItemType && (m_ItemType as DualWieldItemType).PrimaryItem.Equals(item.ItemType)))) {
                ReloadAmmo();
            }
        }

        /// <summary>
        /// Reloads the ammo.
        /// </summary>
        private void ReloadAmmo()
        {
            m_Inventory.ReloadItem(m_ItemType, m_Inventory.GetItemCount(m_ItemType, false));
        }

        /// <summary>
        /// The item should regenerate the ammo.
        /// </summary>
        private void RegenerateAmmo()
        {
            // Only regenerate if there is unloaded ammo.
            if (m_Inventory.GetItemCount(m_ItemType, false) > 0) {
                var amount = m_RegenerateAmount;
#if UNITY_EDITOR || DLL_RELEASE
                if (amount == 0) {
                    Debug.LogWarning("Warning: RegenerateAmount must be a positive number.");
                    amount = 1;
                }
#endif
                m_Inventory.ReloadItem(m_ItemType, amount);

                // Keep regenerating.
                m_RegenerateEvent = Scheduler.Schedule(m_RegenerateDelay, RegenerateAmmo);
            }
        }

        /// <summary>
        /// Callback from the controller when the item is aimed or no longer aimed.
        /// <param name="aim">Is the controller aiming?</param>
        /// </summary>
        protected override void OnAim(bool aim)
        {
            base.OnAim(aim);

            if (!aim) {
                if (InUse()) {
                    EndUse();
                }
                // When the character is no longer aiming reset the animation states so they will begin fresh.
                m_UseStates.ResetNextState();
            }
        }
    }
}