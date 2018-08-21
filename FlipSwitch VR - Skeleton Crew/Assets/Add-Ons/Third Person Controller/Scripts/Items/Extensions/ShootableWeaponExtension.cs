using UnityEngine;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Any weapon extension that can shopt.
    /// </summary>
    public class ShootableWeaponExtension : WeaponExtension
    {
        /// <summary>
        /// The mode in which the weapon fires multiple shots.
        /// </summary>
        public enum FireMode {
            SemiAuto, // Fire discrete shots, don't continue to fire until the player fires again
            FullAuto, // Keep firing until the ammo runs out or the player stops firing 
            Burst // Keep firing until the burst rate is zero
        }

#if UNITY_EDITOR || DLL_RELEASE
        [Tooltip("Draw a debug line to see the direction that the weapon is firing")]
        [SerializeField] protected bool m_DebugDrawFireRay;
#endif

        [Tooltip("The point at which to do the actual firing")]
        [SerializeField] protected Transform m_FirePoint;
        [Tooltip("The mode in which the weapon fires multiple shots")]
        [SerializeField] protected FireMode m_FireMode = FireMode.SemiAuto;
        [Tooltip("The number of shots per second")]
        [SerializeField] protected float m_FireRate = 2;
        [Tooltip("If using the Burst FireMode, specifies the number of bursts the weapon can fire")]
        [SerializeField] protected int m_BurstRate = 5;
        [Tooltip("The number of rounds to fire in a single shot")]
        [SerializeField] protected int m_FireCount = 1;
        [Tooltip("Should the weapon wait to fire until the used event?")]
        [SerializeField] protected bool m_FireOnUsedEvent;
        [Tooltip("Should the weapon wait for the OnAnimatorItemEndUse to return to a non-use state?")]
        [SerializeField] protected bool m_WaitForEndUseEvent;
        [Tooltip("The amount of recoil to apply when the weapon is fired")]
        [SerializeField] protected float m_RecoilAmount = 0.1f;
        [Tooltip("The random spread of the bullets once they are fired")]
        [SerializeField] protected float m_Spread = 0.01f;
        [Tooltip("The speed at which to regenerate the ammo")]
        [SerializeField] protected float m_RegenerateRate;
        [Tooltip("The amount of ammo to add each regenerative tick. RegenerativeRate must be greater than 0")]
        [SerializeField] protected int m_RegenerateAmount;

        [Tooltip("Optionally specify a shell that should be spawned when the weapon is fired")]
        [SerializeField] protected GameObject m_Shell;
        [Tooltip("If Shell is specified, the location is the position and rotation that the shell spawns at")]
        [SerializeField] protected Transform m_ShellLocation;
        [Tooltip("If Shell is specified, the force is the amount of force applied to the shell when it spawns")]
        [SerializeField] protected Vector3 m_ShellForce;
        [Tooltip("If Shell is specified, the force is the amount of torque applied to the shell when it spawns")]
        [SerializeField] protected Vector3 m_ShellTorque;
        [Tooltip("If the shell is specified, spawn the shell after the specified delay")]
        [SerializeField] protected float m_ShellDelay;

        [Tooltip("Optionally specify a muzzle flash that should appear when the weapon is fired")]
        [SerializeField] protected GameObject m_MuzzleFlash;
        [Tooltip("If Muzzle Flash is specified, the location is the position and rotation that the muzzle flash spawns at")]
        [SerializeField] protected Transform m_MuzzleFlashLocation;

        [Tooltip("Optionally specify any smoke that should appear when the weapon is fired")]
        [SerializeField] protected GameObject m_Smoke;
        [Tooltip("If Smoke is specified, the location is the position and rotation that the smoke spawns at")]
        [SerializeField] protected Transform m_SmokeLocation;

        [Tooltip("Optionally specify any particles that should play when the weapon is fired")]
        [SerializeField] protected ParticleSystem m_FireParticles;

        [Tooltip("Optionally specify a sound that should randomly play when the weapon is fired")]
        [SerializeField] protected AudioClip[] m_FireSound;
        [Tooltip("If Fire Sound is specified, play the sound after the specified delay")]
        [SerializeField] protected float m_FireSoundDelay;
        [Tooltip("Optionally specify a sound that should randomly play when the weapon is fired and out of ammo")]
        [SerializeField] protected AudioClip[] m_EmptyFireSound;

        [Tooltip("Optionally specify a projectile that the weapon should use")]
        [SerializeField] protected GameObject m_Projectile;
        
        [Tooltip("The distance in which the hitscan can reach")]
        [SerializeField] protected float m_HitscanFireRange = float.MaxValue;
        [Tooltip("A LayerMask of the layers that can be hit when fired at. This only applies to weapons that do not have a projectile")]
        [SerializeField] protected LayerMask m_HitscanImpactLayers = -1;
        [Tooltip("Optionally specify an event to send when the hitscan fire hits a target")]
        [SerializeField] protected string m_HitscanDamageEvent;
        [Tooltip("The amount of damage done to the object hit. This only applies to weapons that do not have a projectile")]
        [SerializeField] protected float m_HitscanDamageAmount = 10;
        [Tooltip("How much force is applied to the object hit. This only applies to weapons that do not have a projectile")]
        [SerializeField] protected float m_HitscanImpactForce = 5;
        [Tooltip("Optionally specify a default decal that should be applied to the object hit. This only applies to weapons that do not have a projectile and " +
                 "only be used if no per-object decal is setup in the ObjectManager")]
        [SerializeField] protected GameObject m_DefaultHitscanDecal;
        [Tooltip("Optionally specify any default dust that should appear on top of the object hit. This only applies to weapons that do not have a projectile and " +
                  "be used if no per-object dust is setup in the ObjectManager")]
        [SerializeField] protected GameObject m_DefaultHitscanDust;
        [Tooltip("Optionally specify any default sparks that should appear on top of the object hit. This only applies to weapons that do not have a projectile and " +
                 "only be used if no per-object spark is setup in the ObjectManager")]
        [SerializeField] protected GameObject m_DefaultHitscanSpark;
        [Tooltip("Optionally specify a default impact sound that should play at the point of the object hit. This only applies to weapons that do not have a projectile and " +
                 "only be used if no per-object sound is setup in the ObjectManager")]
        [SerializeField] protected AudioClip m_DefaultHitscanImpactSound;
        [Tooltip("Optionally specify a tracer that should should appear when the hitscan weapon is fired")]
        [SerializeField] protected GameObject m_Tracer;
        [Tooltip("If Tracer is specified, the location is the position and rotation that the tracer spawns at")]
        [SerializeField] protected Transform m_TracerLocation;

        // SharedFields
        private SharedMethod<bool> m_IndependentLook = null;
        private SharedMethod<bool> m_AIAgent = null;
        private SharedProperty<float> m_Recoil = null;
        private SharedMethod<Vector3, bool, Vector3> m_TargetLookDirectionLookPoint = null;

        // Internal variables
        private float m_ShootDelay;
        private float m_LastShootTime;
        private int m_CurrentBurst;
        private bool m_IsFiring;
        private bool m_CanFire;
        private ScheduledEvent m_FireEvent;
        private ScheduledEvent m_EmptyClipEvent;
        private float m_RegenerateDelay;
        private ScheduledEvent m_RegenerateEvent;
        private RaycastHit m_RaycastHit;
        private Collider[] m_CharacterColliders;

        // Component references
        private AudioSource m_AudioSource;
        private Collider[] m_Colliders;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            m_AudioSource = GetComponent<AudioSource>();
            m_Colliders = GetComponents<Collider>();

            m_IsFiring = false;
            m_CanFire = true;
            m_CurrentBurst = m_BurstRate;
            m_ShootDelay = 1.0f / m_FireRate;
            m_LastShootTime = -m_ShootDelay;

            if (m_RegenerateRate > 0) {
                m_RegenerateDelay = 1.0f / m_RegenerateRate;
            }
        }

        /// <summary>
        /// Prepare the item extension for use.
        /// </summary>
        public override void ItemExtensionActivated()
        {
            base.ItemExtensionActivated();

            EventHandler.RegisterEvent(m_GameObject, "OnItemEmptyClip", EmptyClip);
            EventHandler.RegisterEvent(m_GameObject, "OnItemStopEmptyClip", StopEmptyClip);
#if ENABLE_MULTIPLAYER
            EventHandler.RegisterEvent(m_GameObject, "OnItemAddFireEffects", AddFireEffects);
            EventHandler.RegisterEvent<Transform, Vector3, Vector3>(m_GameObject, "OnItemAddHitscanEffects", AddHitscanEffects);
            if (m_Tracer != null) {
                EventHandler.RegisterEvent<Vector3>(m_GameObject, "OnItemAddHitscanTracer", AddHitscanTracer);
            }
#endif
            if (m_WaitForEndUseEvent) {
                EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
            }
        }

        /// <summary>
        /// The item extension has been unequipped.
        /// </summary>
        public override void ItemExtensionDeactivated()
        {
            base.ItemExtensionDeactivated();

            EventHandler.UnregisterEvent(m_GameObject, "OnItemEmptyClip", EmptyClip);
            EventHandler.UnregisterEvent(m_GameObject, "OnItemStopEmptyClip", StopEmptyClip);
#if ENABLE_MULTIPLAYER
            EventHandler.UnregisterEvent(m_GameObject, "OnItemAddFireEffects", AddFireEffects);
            EventHandler.UnregisterEvent<Transform, Vector3, Vector3>(m_GameObject, "OnItemAddHitscanEffects", AddHitscanEffects);
            if (m_Tracer != null) {
                EventHandler.UnregisterEvent<Vector3>(m_GameObject, "OnItemAddHitscanTracer", AddHitscanTracer);
            }
#endif
            if (m_WaitForEndUseEvent) {
                EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemEndUse", EndUse);
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

            // When the projectile is instantiated it should ignore all of the character's colliders.
            if (m_Projectile != null) {
                m_CharacterColliders = m_Character.GetComponentsInChildren<Collider>(true);
            }

            // Register for character events within Init to allow the weapon to recieve the callback even when the weapon isn't active. This allows
            // the character to pickup ammo for a weapon before picking up the weapon and having the ammo already loaded.
            EventHandler.RegisterEvent<Item, bool, bool>(m_Character, "OnInventoryConsumableItemCountChange", ConsumableItemCountChange);
            EventHandler.RegisterEvent<Item>(m_Character, "OnInventoryPrimaryItemChange", PrimaryItemChange);

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
        }
        
        /// <summary>
        /// Try to fire the weapon. The weapon may not be able to be fired for various reasons such as it already firing or it being out of ammo.
        /// </summary>
        public override bool TryUse()
        {
            if (!m_IsFiring && m_CanFire && m_LastShootTime + m_ShootDelay < Time.time) {
                if (m_Inventory.GetItemCount(m_ConsumableItemType) > 0) {
                    m_IsFiring = true;
                    // Prevent the weapon from continuously firing if it not a fully automatic. AI agents do not have to follow this because they don't manually stop firing.
                    m_CanFire = m_FireMode == FireMode.FullAuto || m_AIAgent.Invoke();

                    // Do not regenerate any more ammo after starting to fire.
                    if (m_RegenerateEvent != null) {
                        Scheduler.Cancel(ref m_RegenerateEvent);
                    }

                    // Wait until the used event is called before firing.
                    if (!m_FireOnUsedEvent) {
                        DoFire();
                    } else {
                        EventHandler.ExecuteEvent(m_Character, "OnUpdateAnimator");
                    }

                    return true;
                } else {
#if ENABLE_MULTIPLAYER
                    m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemEmptyClip");
#else
                    EmptyClip();
#endif
                }
            }
            return false;
        }

        /// <summary>
        /// The weapon no longer has any ammo. Play the empty clip audio.
        /// </summary>
        private void EmptyClip()
        {
            // Don't play another empty clip sound effect if it is already scheduled.
            if (m_EmptyClipEvent != null) {
                Scheduler.Cancel(ref m_EmptyClipEvent);
            }

            if (m_EmptyFireSound != null && m_EmptyFireSound.Length > 0 && !m_AudioSource.isPlaying) {
                m_AudioSource.clip = m_EmptyFireSound[Random.Range(0, m_EmptyFireSound.Length)];
                m_AudioSource.Play();
            }

            // Keep repeating until the stop used event is called.
            if ((m_FireMode != FireMode.SemiAuto && m_Inventory.GetItemCount(m_ConsumableItemType, true) == 0) || (m_FireMode == FireMode.Burst && m_CurrentBurst == 0)) {
                m_EmptyClipEvent = Scheduler.Schedule(m_ShootDelay, EmptyClip);
            } else {
                EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
            }
        }

        /// <summary>
        /// Stops the empty clip from playing.
        /// </summary>
        private void StopEmptyClip()
        {
            Scheduler.Cancel(ref m_EmptyClipEvent);
        }

        /// <summary>
        /// Can the item be used?
        /// </summary>
        /// <returns>True if the item can be used.</returns>
        public override bool CanUse()
        {
            return m_CanFire;
        }

        /// <summary>
        /// Is the weapon currently being fired?
        /// </summary>
        /// <returns>True if the weapon is firing.</returns>
        public override bool InUse()
        {
            return m_IsFiring; 
        }

        /// <summary>
        /// The weapon has been used. Stop using the item if the fire type is instant or out of ammo. Do a fire if the item should first be charged.
        /// </summary>
        public override void Used()
        {
            if (m_FireEvent == null || m_Inventory.GetItemCount(m_ConsumableItemType) == 0) {
                if (m_FireOnUsedEvent && m_Inventory.GetItemCount(m_ConsumableItemType) > 0) {
                    DoFire();
                }
                if (!m_WaitForEndUseEvent) {
                    StopFiring(true);
                }
            }
        }

        /// <summary>
        /// Stop the weapon from firing.
        /// </summary>
        public override void TryStopUse()
        {
            m_CanFire = true;

            // The weapon is no longer being fired. Cancel the empty clip event.
            if (m_EmptyClipEvent != null) {
#if ENABLE_MULTIPLAYER
                m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemStopEmptyClip");
#else
                StopEmptyClip();
#endif
            }

            // Don't stop firing if waiting for an end use event or the weapon will stop firing by itself.
            if (m_WaitForEndUseEvent || m_FireMode == FireMode.SemiAuto) {
                return;
            }

            StopFiring(false);
        }

        /// <summary>
        /// Do the actual fire.
        /// </summary>
        private void DoFire()
        {
            EventHandler.ExecuteEvent(m_Character, "OnItemUse");
            m_LastShootTime = Time.time;
            m_FireEvent = null;

            // Fire as many projectiles or hitscan bullets as the fire count specifies.
            for (int i = 0; i < m_FireCount; ++i) {
                Fire();
            }

            // Decrement the amount of ammo from the inventory.
            m_Inventory.UseItem(m_ConsumableItemType, 1);

            // Add any fire effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
            m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemAddFireEffects");
#else
            AddFireEffects();
#endif

            // Determine the the weapon should be fired again.  IsFiring may be false if the weapon was stopped because of the fire event.
            var repeatFire = m_IsFiring && m_FireMode != FireMode.SemiAuto;
            if (m_FireMode == FireMode.Burst) {
                m_CurrentBurst--;
                if (m_CurrentBurst == 0) {
                    repeatFire = false;
                }
            }
            
            // Fire again if necessary.
            if (repeatFire && m_Inventory.GetItemCount(m_ConsumableItemType) > 0) {
                m_FireEvent = Scheduler.Schedule(m_ShootDelay, DoFire);
            } else if (m_Inventory.GetItemCount(m_ConsumableItemType) == 0) {
                // Out of ammo.
#if ENABLE_MULTIPLAYER
                m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemEmptyClip");
#else
                EmptyClip();
#endif
            }
        }

        /// <summary>
        /// A weapon has been fired, add any fire effects.
        /// </summary>
        private void AddFireEffects()
        {
            // Apply a recoil.
            if (m_Recoil != null) {
                m_Recoil.Set(m_RecoilAmount);
            }

            // Spawn a shell.
            if (m_Shell) {
                if (m_ShellDelay > 0) {
                    Scheduler.Schedule(m_ShellDelay, SpawnShell);
                } else {
                    SpawnShell();
                }
            }

            // Spawn a muzzle flash.
            if (m_MuzzleFlash) {
                // Choose a random z rotation angle.
                var eulerAngles = m_MuzzleFlashLocation.eulerAngles;
                eulerAngles.z = Random.Range(0, 360);
                var muzzleFlashObject = ObjectPool.Instantiate(m_MuzzleFlash, m_MuzzleFlashLocation.position, Quaternion.Euler(eulerAngles), m_Transform);
                MuzzleFlash muzzleFlash;
                if ((muzzleFlash = Utility.GetComponentForType<MuzzleFlash>(muzzleFlashObject)) != null) {
                    muzzleFlash.Show();
                }
            }

            // Spawn any smoke.
            if (m_Smoke) {
                ObjectPool.Instantiate(m_Smoke, m_SmokeLocation.position, m_SmokeLocation.rotation);
            }

            // Play any particle effects.
            if (m_FireParticles) {
                m_FireParticles.Play(true);
            }

            // Play a firing sound.
            if (m_FireSound != null && m_FireSound.Length > 0) {
                m_AudioSource.clip = m_FireSound[Random.Range(0, m_FireSound.Length)];
                if (m_FireSoundDelay > 0) {
                    m_AudioSource.PlayDelayed(m_FireSoundDelay);
                } else {
                    m_AudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Spawns the shell.
        /// </summary>
        private void SpawnShell()
        {
            var shell = ObjectPool.Instantiate(m_Shell, m_ShellLocation.position, m_ShellLocation.rotation);
            var shellRigidbody = Utility.GetComponentForType<Rigidbody>(shell);
            shellRigidbody.AddRelativeForce(m_ShellForce);
            shellRigidbody.AddRelativeTorque(m_ShellTorque);
        }

        /// <summary>
        /// Virtual method to actually fire the weapon. Will fire a projectile if it exists, otherwise a hitscan fire will be used.
        /// </summary>
        protected virtual void Fire()
        {
            // Fire a projectile if it exists, otherwise fire a raycast.
            if (m_Projectile) {
                ProjectileFire();
            } else {
                HitscanFire();
            }
        }

        /// <summary>
        /// Spawns a projectile which will move in the firing direction.
        /// </summary>
        protected virtual void ProjectileFire()
        {
            var rotation = Quaternion.LookRotation(FireDirection());
            var projectileGameObject = ObjectPool.Spawn(m_Projectile, m_FirePoint.position, rotation * m_Projectile.transform.rotation);
            var projectile = Utility.GetComponentForType<Projectile>(projectileGameObject);
            projectile.Initialize(rotation * Vector3.forward, Vector3.zero, m_Character);
            var projectileCollider = Utility.GetComponentForType<Collider>(projectileGameObject);

            // Ignore all of the colliders to prevent the projectile from detonating as a result of the character. 
            if (projectileCollider != null) {
                for (int i = 0; i < m_Colliders.Length; ++i) {
                    LayerManager.IgnoreCollision(projectileCollider, m_Colliders[i]);
                }
                for (int i = 0; i < m_CharacterColliders.Length; ++i) {
                    LayerManager.IgnoreCollision(projectileCollider, m_CharacterColliders[i]);
                }
            }
        }

        /// <summary>
        /// Fire by casting a ray in the specified direction. If an object was hit apply the damage, apply a force, add a decal, etc.
        /// </summary>
        protected virtual void HitscanFire()
        {
            // Cast a ray between the fire point and the position found by the crosshairs camera ray.
            var fireDirection = FireDirection();
            if (Physics.Raycast(m_FirePoint.position, fireDirection, out m_RaycastHit, m_HitscanFireRange, m_HitscanImpactLayers.value, QueryTriggerInteraction.Ignore)) {

                // Execute any custom events.
                if (!string.IsNullOrEmpty(m_HitscanDamageEvent)) {
                    EventHandler.ExecuteEvent(m_RaycastHit.collider.gameObject, m_HitscanDamageEvent, m_HitscanDamageAmount, m_RaycastHit.point, m_RaycastHit.normal * -m_HitscanImpactForce, m_Character);
                }

                // If the Health component exists it will apply a force to the rigidbody in addition to deducting the health. Otherwise just apply the force to the rigidbody. 
                Health hitHealth;
                if ((hitHealth = m_RaycastHit.transform.GetComponentInParent<Health>()) != null) {
                    hitHealth.Damage(m_HitscanDamageAmount, m_RaycastHit.point, fireDirection * m_HitscanImpactForce, m_Character, m_RaycastHit.transform.gameObject);
                } else if (m_HitscanImpactForce > 0 && m_RaycastHit.rigidbody != null && !m_RaycastHit.rigidbody.isKinematic) {
                    m_RaycastHit.rigidbody.AddForceAtPosition(fireDirection * m_HitscanImpactForce, m_RaycastHit.point);
                }

                // Add any hitscan effects. These effects do not need to be added on the server.
#if ENABLE_MULTIPLAYER
                m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemAddHitscanEffects", m_RaycastHit.transform.gameObject, m_RaycastHit.point, m_RaycastHit.normal);
#else
                AddHitscanEffects(m_RaycastHit.transform, m_RaycastHit.point, m_RaycastHit.normal);
#endif
            } else if (m_Tracer) {
                // An object was not hit but a tracer should still fire.
#if ENABLE_MULTIPLAYER
                m_NetworkMonitor.ExecuteItemEvent(m_ConsumableItemType.ID, "OnItemAddHitscanTracer", m_FirePoint.position + fireDirection * 1000);
#else
                AddHitscanTracer(m_FirePoint.position + fireDirection * 1000);
#endif
            }
        }

        /// <summary>
        /// The hitscan has hit an object, add any hitscan effects.
        /// </summary>
        /// <param name="hitTransform">The transform that was hit.</param>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="hitNormal">The normal of the transform at the hit point.</param>
        private void AddHitscanEffects(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal)
        {
            var hitRotation = Quaternion.LookRotation(hitNormal);

            Object retrievedObject = null;
            // Don't add the decal if the hit layer doesnt allow decals (such as other characters).
            if (DecalManager.CanAddDecal(hitTransform.gameObject.layer)) {
                GameObject decal;
                if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Decal, ref retrievedObject)) {
                    decal = retrievedObject as GameObject;
                } else {
                    decal = m_DefaultHitscanDecal;
                }
                if (decal != null) {
                    // Apply a decal to the hit point. Offset the decal by a small amount so it doesn't interset with the object hit.
                    DecalManager.Add(decal, hitPoint + hitNormal * 0.02f, decal.transform.rotation * hitRotation, hitTransform);
                }
            }

            // Spawn a dust particle effect at the hit point.
            GameObject dust;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Dust, ref retrievedObject)) {
                dust = retrievedObject as GameObject;
            } else {
                dust = m_DefaultHitscanDust;
            }
            if (dust != null) {
                ObjectPool.Instantiate(dust, hitPoint, dust.transform.rotation * hitRotation);
            }

            // Spawn a spark particle effect at the hit point.
            GameObject spark;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Spark, ref retrievedObject)) {
                spark = retrievedObject as GameObject;
            } else {
                spark = m_DefaultHitscanDust;
            }
            if (spark != null) {
                ObjectPool.Instantiate(spark, hitPoint, spark.transform.rotation * hitRotation);
            }

            // Play a sound at the hit point.
            AudioClip audioClip;
            if (ObjectManager.ObjectForItem(hitTransform.tag, m_ConsumableItemType, ObjectManager.ObjectCategory.Audio, ref retrievedObject)) {
                audioClip = retrievedObject as AudioClip;
            } else {
                audioClip = m_DefaultHitscanImpactSound;
            }
            if (audioClip != null) {
                AudioSource.PlayClipAtPoint(audioClip, hitPoint);
            }

            // Spawn a tracer which moves to the hit point.
            if (m_Tracer != null) {
                AddHitscanTracer(hitPoint);
            }
        }

        /// <summary>
        /// Adds a tracer to the hitscan weapon.
        /// </summary>
        /// <param name="position">The position that the tracer should move towards.</param>
        private void AddHitscanTracer(Vector3 position)
        {
            var tracerObject = ObjectPool.Instantiate(m_Tracer, m_TracerLocation.position, m_TracerLocation.rotation);
            var tracer = Utility.GetComponentForType<Tracer>(tracerObject);
            if (tracer != null) {
                tracer.Initialize(position);
            }
        }

#if UNITY_EDITOR || DLL_RELEASE
        public void Update()
        {
            if (m_DebugDrawFireRay) {
                Debug.DrawRay(m_FirePoint.position, FireDirection() * 100, Color.red);
            }
        }
#endif

        /// <summary>
        /// Determines the direction to fire based on the camera's look position and a random spread.
        /// </summary>
        /// <returns>The direction to fire.</returns>
        private Vector3 FireDirection()
        {
            Vector3 direction;
            // If m_TargetLookDirectionLookPoint is null then use the forward direction. It may be null if the AI agent doesn't have the AIAgent component attached.
            if (m_TargetLookDirectionLookPoint == null) {
                direction = m_FirePoint.forward;
            } else {
                direction = m_TargetLookDirectionLookPoint.Invoke(m_FirePoint.position, true);
                // Don't let the character fire in the opposite direction of the weapon.
                if (Vector3.Dot(m_Character.transform.forward, direction) < 0) {
                    direction = m_FirePoint.forward;
                }
            }

            // Add a random spread.
            if (m_Spread > 0) {
                var variance = Quaternion.AngleAxis(Random.Range(0, 360), direction) * Vector3.up * Random.Range(0, m_Spread);
                direction += variance;
            }

            return direction;
        }

        /// <summary>
        /// Stop firing the weapon. Cancel and reset the affected variables.
        /// </summary>
        /// <param name="success">Did the item successfully fire?</param>
        private void StopFiring(bool success)
        {
            // Can't stop firing if the weapon isn't firing to begin with.
            if (!m_IsFiring) {
                EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
                return;
            }

            m_IsFiring = false;
            m_CurrentBurst = m_BurstRate;
            m_UseStates.NextState();

            if (m_FireEvent != null) {
                Scheduler.Cancel(ref m_FireEvent);
            }

            // Keep repeating the empty clip method until the stop used event is called.
            if (m_EmptyClipEvent == null && success &&
                ((m_FireMode != FireMode.SemiAuto && m_Inventory.GetItemCount(m_ConsumableItemType, true) == 0) || (m_FireMode == FireMode.Burst && m_CurrentBurst == 0))) {
                m_EmptyClipEvent = Scheduler.Schedule(m_ShootDelay, EmptyClip);
            }

            if (m_FireParticles) {
                m_FireParticles.Stop();
            }

            if (m_RegenerateDelay != 0) {
                m_RegenerateEvent = Scheduler.Schedule(m_RegenerateDelay, RegenerateAmmo);
            }
            
            EventHandler.ExecuteEvent(m_Character, "OnItemStopUse");
        }

        /// <summary>
        /// Ends the weapon use.
        /// </summary>
        private void EndUse()
        {
            StopFiring(true);
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
                    StopFiring(false);
                }
                // When the character is no longer aiming reset the animation states so they will begin fresh.
                m_UseStates.ResetNextState();
            }
        }

        /// <summary>
        /// Event notification when the inventory has changed its primary item. Stop firing if the weapon is currently firing.
        /// </summary>
        /// <param name="item">The new item. Can be null.</param>
        private void PrimaryItemChange(Item item)
        {
            StopFiring(false);
        }

        /// <summary>
        /// Event notification that the Inventory has added or removed consumable items. Determine if the weapon needs to be reloaded.
        /// </summary>
        /// <param name="item">The item whose consumable ammo has changed.</param>
        /// <param name="added">True if the consumable items were added.</param>
        /// <param name="immediateChange">True if the consumable item count should be changed immediately. This will be true when the player initially spawns.</param>
        private void ConsumableItemCountChange(Item item, bool added, bool immediateChange)
        {
            // DualWield items require the GameObject to be active. The DualWielded item may never be picked up so it doesn't need to take ammo from the PrimaryItem.
            if (added && (item.ItemType.Equals(m_ParentItem.ItemType) || 
                            (gameObject.activeSelf && m_ParentItem.ItemType is DualWieldItemType && (m_ParentItem.ItemType as DualWieldItemType).PrimaryItem.Equals(item.ItemType)))) {
                ReloadComplete(); // There are no reload animations with ShootableWeaponExtension.
            }
        }

        /// <summary>
        /// Does the actual reload.
        /// </summary>
        private void ReloadComplete()
        {
            m_Inventory.ReloadItem(m_ConsumableItemType, m_Inventory.GetItemCount(m_ConsumableItemType, false));
        }

        /// <summary>
        /// The weapon should regenerate the ammo.
        /// </summary>
        private void RegenerateAmmo()
        {
            // Only regenerate if there is unloaded ammo.
            if (m_Inventory.GetItemCount(m_ConsumableItemType, false) > 0) {
                var amount = m_RegenerateAmount;
#if UNITY_EDITOR || DLL_RELEASE
                if (amount == 0) {
                    Debug.LogWarning("Warning: RegenerateAmount must be a positive number.");
                    amount = 1;
                }
#endif
                m_Inventory.ReloadItem(m_ConsumableItemType, amount);

                // Keep regenerating.
                m_RegenerateEvent = Scheduler.Schedule(m_RegenerateDelay, RegenerateAmmo);
            }
        }
    }
}