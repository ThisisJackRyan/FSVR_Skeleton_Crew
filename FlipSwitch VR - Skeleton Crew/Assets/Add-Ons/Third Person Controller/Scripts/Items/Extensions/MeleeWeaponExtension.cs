using UnityEngine;
using System.Collections.Generic;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Any weapon extension that uses melee to damage the target.
    /// </summary>
    public class MeleeWeaponExtension : WeaponExtension, IHitboxItem
    {
        [Tooltip("The state to play if the melee weapon hits a fixed object")]
        [SerializeField] protected AnimatorItemCollectionData m_RecoilStates = new AnimatorItemCollectionData("Recoil", "Recoil", 0.2f, true);

        [Tooltip("The number of melee attacks per second")]
        [SerializeField] protected float m_AttackRate = 2;
        [Tooltip("The layers that the melee attack can hit")]
        [SerializeField] protected LayerMask m_AttackLayer;
        [Tooltip("Any other hitboxes that should be used when determining if the melee weapon hit a target")]
        [SerializeField] protected MeleeWeaponHitbox[] m_AttackHitboxes;
        [Tooltip("Can the attack be interrupted to move onto the next attack? The OnAnimatorItemAllowInterruption event must be added to the attack animation")]
        [SerializeField] protected bool m_CanInterruptAttack;
        [Tooltip("When the weapon attacks should only one hit be registered per use?")]
        [SerializeField] protected bool m_SingleHitAttack;
        [Tooltip("Should the weapon wait for the OnAnimatorItemEndUse to return to a non-use state?")]
        [SerializeField] protected bool m_WaitForEndUseEvent;

        [Tooltip("Optionally specify a sound that should randomly play when the weapon is attacked")]
        [SerializeField] protected AudioClip[] m_AttackSound;
        [Tooltip("If Attack Sound is specified, play the sound after the specified delay")]
        [SerializeField] protected float m_AttackSoundDelay;

        [Tooltip("Optionally specify an event to send to the object hit on damage")]
        [SerializeField] protected string m_DamageEvent;
        [Tooltip("The amount of damage done to the object hit")]
        [SerializeField] protected float m_DamageAmount = 10;
        [Tooltip("How much force is applied to the object hit")]
        [SerializeField] protected float m_ImpactForce = 5;
        [Tooltip("Optionally specify any default dust that should appear on at the location of the object hit. This is only used if no per-object dust is setup in the ObjectManager")]
        [SerializeField] protected GameObject m_DefaultDust;
        [Tooltip("Optionally specify a default impact sound that should play at the point of the object hit. This is only used if no per-object sound is setup in the ObjectManager")]
        [SerializeField] protected AudioClip m_DefaultImpactSound;

        // SharedFields
        private SharedMethod<bool> m_IndependentLook = null;

        // Internal variables
        private float m_AttackDelay;
        private float m_LastAttackTime;
        private HashSet<Health> m_HitList = new HashSet<Health>();
        private bool m_InUse;
        private bool m_AllowInterruption;
        private bool m_AttackHit;
        private bool m_Recoil;

        // Component references
        private AudioSource m_AudioSource;
        private Rigidbody m_CharacterRigidbody;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            m_AudioSource = GetComponent<AudioSource>();

            m_AttackDelay = 1.0f / m_AttackRate;
            m_LastAttackTime = -m_AttackRate;

            // Register any hitboxes with the current MeleeWeapon.
            for (int i = 0; i < m_AttackHitboxes.Length; ++i) {
                m_AttackHitboxes[i].Owner = this;
                m_AttackHitboxes[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Prepare the item extension for use.
        /// </summary>
        public override void ItemExtensionActivated()
        {
            base.ItemExtensionActivated();

#if ENABLE_MULTIPLAYER
            EventHandler.RegisterEvent<Transform, Vector3, Vector3>(m_GameObject, "OnItemAddMeleeEffects", AddMeleeEffects);
            EventHandler.RegisterEvent(m_GameObject, "OnItemAddAttackEffects", AddAttackEffects);
#endif
            EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
            EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndRecoil", EndRecoil);
            if (m_CanInterruptAttack) {
                EventHandler.RegisterEvent(m_Character, "OnAnimatorItemAllowInterruption", AllowInterruption);
            }

            // Activate any colliders and hitboxes.
            for (int i = 0; i < m_AttackHitboxes.Length; ++i) {
                m_AttackHitboxes[i].gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// The item extension has been unequipped.
        /// </summary>
        public override void ItemExtensionDeactivated()
        {
            base.ItemExtensionDeactivated();

            // The animation states should begin fresh.
            m_RecoilStates.ResetNextState();

#if ENABLE_MULTIPLAYER
            EventHandler.UnregisterEvent<Transform, Vector3, Vector3>(m_GameObject, "OnItemAddMeleeEffects", AddMeleeEffects);
            EventHandler.UnregisterEvent(m_GameObject, "OnItemAddAttackEffects", AddAttackEffects);
#endif
            EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
            EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemEndRecoil", EndRecoil);
            if (m_CanInterruptAttack) {
                EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemAllowInterruption", AllowInterruption);
            }

            for (int i = 0; i < m_AttackHitboxes.Length; ++i) {
                m_AttackHitboxes[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initializes the item after it has been added to the Inventory.
        /// </summary>
        /// <param name="item">The item that this extension belongs to.</param>
        /// <param name="inventory">The parent character's inventory.</param>
        public override void Init(Item item, Inventory inventory)
        {
            base.Init(item, inventory);

            m_CharacterRigidbody = inventory.GetComponent<Rigidbody>();

            // Register for character events if the GameObject is active. OnEnable normally registers for these callbacks but in this case OnEnable has already occurred.
            if (m_GameObject.activeSelf) {
                EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
            }

            SharedManager.InitializeSharedFields(m_Character, this);
            // Independent look characters do not need to communicate with the camera. Do not initialze the SharedFields on the network to prevent non-local characters from
            // using the main camera to determine their look direction. The SharedFields have been implemented by the NetworkMonitor component.
#if ENABLE_MULTIPLAYER
            if (!m_IndependentLook.Invoke() && m_IsLocalPlayer.Invoke()) {
#else
            if (!m_IndependentLook.Invoke()) {
#endif
                SharedManager.InitializeSharedFields(Utility.FindCamera(m_Character).gameObject, this);
            }

            // Initialize the animation states.
            m_RecoilStates.Initialize(m_ParentItem.ItemType);
        }

        /// <summary>
        /// Returns the destination state for the given layer.
        /// </summary>
        /// <param name="priority">Specifies the item animation priority to retrieve. High priority animations get tested before lower priority animations.</param>
        /// <param name="layer">The Animator layer index.</param>
        /// <returns>The state that the Animator should be in for the given layer. A null value indicates no change.</returns>
        public override AnimatorItemStateData GetDestinationState(Item.ItemAnimationPriority priority, int layer)
        {
            var state = base.GetDestinationState(priority, layer);
            if (state != null) {
                return state;
            }

            // Any animation called by the MeleeWeaponExtension component is a high priority animation.
            if (priority == Item.ItemAnimationPriority.High) {
                if (m_Recoil) {
                    state = m_RecoilStates.GetState(layer, m_Controller.Moving);
                    if (state != null) {
                        return state;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Can the weapon be meleed?
        /// </summary>
        /// <returns>True if the weapon can be meleed.</returns>
        public override bool CanUse()
        {
            if (!base.CanUse()) {
                return false;
            }
            return !m_InUse || m_AllowInterruption;
        }

        /// <summary>
        /// Try to attack. The weapon may not be able to attack if the last attack was too recent.
        /// <returns>True if the item was used.</returns>
        /// </summary>
        public override bool TryUse()
        {
            // End the item use if the weapon is currently being used and can be interrupted. This will allow the next attack to play.
            if (m_InUse && m_AllowInterruption) {
                EndUse();
            }

            if (!m_InUse && m_LastAttackTime + m_AttackDelay < Time.time) {
                m_LastAttackTime = Time.time;
                m_InUse = true;
                m_AttackHit = false;
                m_HitList.Clear();
                // Add any melee starting effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
                m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemAddAttackEffects");
#else
                AddAttackEffects();
#endif
                EventHandler.ExecuteEvent(m_Character, "OnItemUse");
                return true;
            }
            return false;
        }

        /// <summary>
        /// The melee weapon has attacked, add any effects.
        /// </summary>
        private void AddAttackEffects()
        {
            // Play a attack sound.
            if (m_AttackSound != null && m_AttackSound.Length > 0) {
                m_AudioSource.clip = m_AttackSound[Random.Range(0, m_AttackSound.Length - 1)];
                if (m_AttackSoundDelay > 0) {
                    m_AudioSource.PlayDelayed(m_AttackSoundDelay);
                } else {
                    m_AudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Is the melee weapon currently being used?
        /// </summary>
        /// <returns>True if the weapon is in use.</returns>
        public override bool InUse()
        {
            return m_InUse;
        }

        /// <summary>
        /// Attack the specified object.
        /// </summary>
        /// <param name="hitTransform">The Transform of the hit object.</param>
        /// <param name="hitPoint">The position of the collision.</param>
        /// <param name="hitNormal">The normal of the collision.</param>
        protected virtual void Attack(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Don't pass on the hit if the attack has already hit an object.
            if (m_SingleHitAttack) {
                if (m_AttackHit) {
                    return;
                }
                m_AttackHit = true;
            }

            var hitHealth = hitTransform.GetComponentInParent<Health>();
            Rigidbody hitRigidbody;
            // If the Health component exists it will apply a force to the rigidbody in addition to deducting the health. Otherwise just apply the force to the rigidbody. 
            if (hitHealth != null) {
                // Don't allow the same GameObejct to be hit multiple times. This will occur if the GameObject has multiple colliders.
                if (m_HitList.Contains(hitHealth)) {
                    return;
                }
                m_HitList.Add(hitHealth);
                hitHealth.Damage(m_DamageAmount, hitPoint, hitNormal * -m_ImpactForce, m_Character, hitTransform.gameObject);
            } else if (m_ImpactForce > 0 && (hitRigidbody = hitTransform.GetComponent<Rigidbody>()) != null && !hitRigidbody.isKinematic) {
                hitRigidbody.AddForceAtPosition(hitNormal * -m_ImpactForce, hitPoint);
            }

            // Execute any custom events.
            if (!string.IsNullOrEmpty(m_DamageEvent)) {
                EventHandler.ExecuteEvent(hitTransform.gameObject, m_DamageEvent, m_DamageAmount, hitPoint, hitNormal * -m_ImpactForce, m_Character);
            }

            // Add any melee effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
            m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemAddMeleeEffects", hitTransform.gameObject, hitPoint, hitNormal);
#else
            AddMeleeEffects(hitTransform, hitPoint, hitNormal);
#endif
        }

        /// <summary>
        /// Ends the weapon use.
        /// </summary>
        private void EndUse()
        {
            if (!m_InUse) {
                return;
            }
            m_InUse = false;
            m_AllowInterruption = false;
            m_UseStates.NextState();
            m_RecoilStates.NextState();
            EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
        }

        /// <summary>
        /// The melee hit an object, add any melee effects.
        /// </summary>
        /// <param name="hitTransform">The transform that was hit.</param>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="hitNormal">The normal of the transform at the hit point.</param>
        private void AddMeleeEffects(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal)
        {
            Object retrievedObject = null;
            // Spawn a dust particle effect at the hit point.
            GameObject dust;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Dust, ref retrievedObject)) {
                dust = retrievedObject as GameObject;
            } else {
                dust = m_DefaultDust;
            }
            if (dust != null) {
                ObjectPool.Instantiate(dust, hitPoint, dust.transform.rotation * Quaternion.LookRotation(hitNormal));
            }

            // Play a sound at the hit point.
            AudioClip audioClip;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Audio, ref retrievedObject)) {
                audioClip = retrievedObject as AudioClip;
            } else {
                audioClip = m_DefaultImpactSound;
            }
            if (audioClip != null) {
                AudioSource.PlayClipAtPoint(audioClip, hitPoint);
            }
        }

        /// <summary>
        /// Allows the attack animation to be interrupted.
        /// </summary>
        private void AllowInterruption()
        {
            if (m_InUse) {
                m_AllowInterruption = true;
            }
        }

        /// <summary>
        /// The collider has collided with another object. Perform the attack if using the physics attack type.
        /// </summary>
        /// <param name="collision">The object that collided with the MeleeWeapon.</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (m_InUse) {
#if ENABLE_MULTIPLAYER
                // The server will control the raycast logic.
                if (!m_IsServer.Invoke()) {
                    return;
                }
#endif
                if (Utility.InLayerMask(collision.gameObject.layer, m_AttackLayer.value)) {
                    Attack(collision.transform, collision.contacts[0].point, collision.contacts[0].normal);
                }

                // The character should play a recoil animation if the object does not have a Rigidbody, the Rigidbody is kinematic, or the Rigidbody is much heavier than the character.
                if (collision.rigidbody == null || collision.rigidbody.isKinematic || collision.rigidbody.mass > m_CharacterRigidbody.mass * 10) {
                    if (!m_WaitForEndUseEvent) {
                        EndUse();
                    }

                    var recoilState = m_RecoilStates.GetState(m_AnimatorMonitor.UpperLayerIndex, m_Controller.Moving);
                    if (recoilState != null && !string.IsNullOrEmpty(recoilState.Name)) {
                        m_Recoil = true;
                        EventHandler.ExecuteEvent(m_Character, "OnUpdateAnimator");
                    }
                }
            }
        }

        /// <summary>
        /// The hitbox collided with another object.
        /// </summary>
        /// <param name="other">The object that collided with the hitbox.</param>
        public void HitboxCollision(Collision collision)
        {
            OnCollisionEnter(collision);
        }

        /// <summary>
        /// The recoil animation has ended.
        /// </summary>
        private void EndRecoil()
        {
            if (m_Recoil) {
                m_Recoil = false;
                EventHandler.ExecuteEvent(m_Character, "OnUpdateAnimator");
                m_UseStates.ResetNextState();
                m_RecoilStates.ResetNextState();
            }
        }

        /// <summary>
        /// Callback from the controller when the item is aimed or no longer aimed.
        /// <param name="aim">Is the controller aiming?</param>
        /// </summary>
        protected override void OnAim(bool aim)
        {
            if (!aim) {
                if (InUse()) {
                    EndUse();
                }
            }
        }
    }
}