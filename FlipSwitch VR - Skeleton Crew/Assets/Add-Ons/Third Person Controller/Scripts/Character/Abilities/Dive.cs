using UnityEngine;

namespace Opsive.ThirdPersonController.Abilities
{
    /// <summary>
    /// The Dive ability allows the character to swim while below water. The Dive ability will only start when the Swim ability
    /// is active or in the case of a completely underwater scene will start when requested if there is no Swim ability.
    /// The Dive ability will stop when the character reaches the surface of the water.
    /// </summary>
    public class Dive : Ability
    {
        [Tooltip("The speed the character will move underwater at")]
        [SerializeField] protected float m_DiveSpeed = 1f;
        [Tooltip("The minimum water depth that the dive ability can start")]
        [SerializeField] protected float m_MinDiveDepth = 2f;
        [Tooltip("Prevent the character from diving again too quickly after stopping the ability")]
        [SerializeField] protected float m_RecurrenceDelay = 1f;
        [Tooltip("The height adjustment of the collider")]
        [SerializeField] protected float m_ColliderHeightAdjustment;

        // Component references
        private Rigidbody m_Rigidbody;
        private Swim m_Swim;

        private float m_StartDiveHeight;
        private float m_LastDiveStop;
        private bool m_DiveStarted;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_Rigidbody = GetComponent<Rigidbody>();
            m_Swim = GetComponent<Swim>();
        }

        /// <summary>
        /// Can the ability be started?
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            // If the swimming ability exists then Dive can only be started if swim is active.
            if (m_Swim != null && !m_Swim.IsActive) {
                return false;
            }

            // Prevent the dive ability from starting again too quickly.
            if (m_LastDiveStop + m_RecurrenceDelay > Time.time) {
                return false;
            }
            
            // Prevent the character from diving in shallow water.
            RaycastHit hit;
            if (Physics.Raycast(m_Controller.transform.position, -m_Controller.transform.up, out hit, m_MinDiveDepth, LayerManager.Mask.IgnoreInvisibleLayersPlayerWater, QueryTriggerInteraction.Ignore)) {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Can the specified ability be started?
        /// </summary>
        /// <param name="ability">The ability that is trying to start.</param>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility(Ability ability)
        {
            // The HeightChange ability cannot start while diving.
            if (ability is HeightChange) {
                return false;
            }

            // The Fly ability cannot start while diving.
            if (ability is Fly) {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Starts executing the ability.
        /// </summary>
        protected override void AbilityStarted()
        {
            // Set the max height value if Swim is null. In this situation the scene is completely underwater
            // so the Dive ability will never stop.
            m_StartDiveHeight = (m_Swim != null ? m_Transform.position.y : float.MaxValue);
            m_Rigidbody.useGravity = false;
            m_DiveStarted = false;
            EventHandler.RegisterEvent(m_GameObject, "OnAnimatorDiveStarted", OnDiveStarted);
            base.AbilityStarted();
        }

        /// <summary>
        /// Callback when dive start animation has completed.
        /// </summary>
        private void OnDiveStarted()
        {
            m_DiveStarted = true;
        }

        /// <summary>
        /// Moves the character according to the input.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its Move method?</returns>
        public override bool Move()
        {
            if (m_DiveStarted && m_Transform.position.y > m_StartDiveHeight) {
                StopAbility();
                if (m_Swim != null) {
                    m_Controller.TryStartAbility(m_Swim);
                }
            }
            return true;
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        protected override void AbilityStopped()
        {
            m_LastDiveStop = Time.time;
            m_Rigidbody.useGravity = true;
            EventHandler.UnregisterEvent(m_GameObject, "OnAnimatorDiveStarted", OnDiveStarted);
            base.AbilityStopped();
        }

        /// <summary>
        /// Apply any movement.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its UpdateMovement method?</returns>
        public override bool UpdateMovement()
        {
            var diveMovement = Vector3.zero;
            diveMovement.Set(m_Controller.RelativeInputVector.x * m_DiveSpeed, 0, m_Controller.RelativeInputVector.z * m_DiveSpeed);
            var velocity = m_Controller.LookRotation * diveMovement * Time.fixedDeltaTime;
            // The dive start animation has the character moving up using root motion.
            velocity.y += m_Controller.RootMotionForce.y;
            // Set the position directly so the full root motion force is applied.
            m_Controller.SetPosition(m_Transform.position + velocity);
            m_Controller.Velocity = Vector3.zero;
            m_Controller.RootMotionForce = Vector3.zero;

            return false;
        }

        /// <summary>
        /// Update the rotation forces.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its UpdateRotation method?</returns>
        public override bool UpdateRotation()
        {
            // Don't apply a rotation if the character hasn't started to dive yet.
            if (!m_DiveStarted) {
                return true;
            }

            // The character is diving. Rotate based on the input.
            if (m_Controller.InputVector != Vector3.zero) {
                var targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(m_Controller.LookRotation * m_Controller.InputVector.normalized).eulerAngles.y, 0);
                m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, targetRotation, m_Controller.RotationSpeed * Time.fixedDeltaTime);
            }
            return false;
        }

        /// <summary>
        /// Update the Animator.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its UpdateAnimator method?</returns>
        public override bool UpdateAnimator()
        {
            if (!m_DiveStarted) {
                return true;
            }
            m_AnimatorMonitor.SetForwardInputValue(m_Controller.InputVector.magnitude);
            return false;
        }

        /// <summary>
        /// Perform checks to determine if the character is on the ground.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its CheckGround method?</returns>
        public override bool CheckGround()
        {
            // Keep the previous ground height at the current height so there is no fall damage when the character lands.
            m_Controller.PrevGroundHeight = m_Transform.position.y;
            return true;
        }

        /// <summary>
        /// Returns the destination state for the given layer.
        /// </summary>
        /// <param name="layer">The Animator layer index.</param>
        /// <returns>The state that the Animator should be in for the given layer. An empty string indicates no change.</returns>
        public override string GetDestinationState(int layer)
        {
            // The ability only affects the base and upper layers.
            if (layer != m_AnimatorMonitor.BaseLayerIndex && layer != m_AnimatorMonitor.UpperLayerIndex)
            {
                return string.Empty;
            }

            return "Dive.Dive Start";
        }

        /// <summary>
        /// Can the character have an item equipped while the ability is active?
        /// </summary>
        /// <returns>True if the character can have an item equipped.</returns>
        public override bool CanHaveItemEquipped()
        {
            return false;
        }

        /// <summary>
        /// The character wants to interact with the item. Return false if there is a reason why the character shouldn't be able to.
        /// </summary>
        /// <returns>True if the item can be interacted with.</returns>
        public override bool CanInteractItem()
        {
            return false;
        }

        /// <summary>
        /// Returns any adjustment applied to the collider height.
        /// </summary>
        /// <returns>The adjustment applied to the collider height.</returns>
        public override float GetColliderHeightAdjustment()
        {
            return m_ColliderHeightAdjustment;
        }
    }
}
