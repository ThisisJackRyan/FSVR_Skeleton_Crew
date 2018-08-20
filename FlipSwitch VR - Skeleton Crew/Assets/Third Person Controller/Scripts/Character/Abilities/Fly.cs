using UnityEngine;

namespace Opsive.ThirdPersonController.Abilities
{
    /// <summary>
    /// The Fly ability allows the character to fly based on look direction.
    /// </summary>
    public class Fly : Ability
    {
        [Tooltip("The speed that the character can fly")]
        [SerializeField] protected float m_FlySpeed = 0.1f;
        [Tooltip("Prevent the character from flying too quickly after stopping flight")]
        [SerializeField] protected float m_RecurrenceDelay = 0.2f;

        // Internal variables
        private float m_LandTime = -1;
        private bool m_Flying;

        // Component references
        private Rigidbody m_Rigidbody;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_Rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Can the ability be started?
        /// </summary>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility()
        {
            return m_Controller.Grounded && m_LandTime + m_RecurrenceDelay < Time.time;
        }

        /// <summary>
        /// Can the specified ability start?
        /// </summary>
        /// <param name="ability">The ability that is trying to start.</param>
        /// <returns>True if the ability can be started.</returns>
        public override bool CanStartAbility(Ability ability)
        {
            if (ability is Dive || ability is HeightChange) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts executing the ability.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            m_Flying = true;
            m_Rigidbody.useGravity = false;
            EventHandler.RegisterEvent<bool>(m_GameObject, "OnControllerGrounded", OnGrounded);
            EventHandler.RegisterEvent(m_GameObject, "OnAnimatorStartFly", OnStartFly);
        }

        /// <summary>
        /// The start fly animation has finished playing so now the Rigidbody should have a force added to it.
        /// </summary>
        private void OnStartFly()
        {
            var velocity = m_Rigidbody.velocity;
            // The velocity may be negative if going down a slope.
            if (velocity.y < 0) {
                velocity.y = 0;
            }
            m_Rigidbody.velocity = velocity;
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
        /// Apply any movement.
        /// </summary>
        /// <returns>Should the RigidbodyCharacterController continue execution of its UpdateMovement method?</returns>
        public override bool UpdateMovement()
        {
            var flyMovement = Vector3.zero;
            flyMovement.Set(m_Controller.RelativeInputVector.x * m_FlySpeed, 0, m_Controller.RelativeInputVector.z * m_FlySpeed);
            var velocity = m_Controller.LookRotation * flyMovement * Time.fixedDeltaTime;
            // The fly start animation has the character moving up using root motion.
            velocity.y += m_Controller.RootMotionForce.y;
            // Set the position directly so the full root motion force is applied.
            m_Controller.SetPosition(m_Transform.position + velocity);
            m_Controller.Velocity = Vector3.zero;
            m_Controller.RootMotionForce = Vector3.zero;

            return false;
        }

        /// <summary>
        /// Returns the destination state for the given layer.
        /// </summary>
        /// <param name="layer">The Animator layer index.</param>
        /// <returns>The state that the Animator should be in for the given layer. An empty string indicates no change.</returns>
        public override string GetDestinationState(int layer)
        {
            // The ability only affects the base and upper layers.
            if (layer != m_AnimatorMonitor.BaseLayerIndex && layer != m_AnimatorMonitor.UpperLayerIndex) {
                return string.Empty;
            }

            return "Fly.Fly Start";
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
            return !m_Flying;
        }

        /// <summary>
        /// The character has either landed or just left the ground.
        /// </summary>
        /// <param name="grounded">Is the character on the ground?</param>
        private void OnGrounded(bool grounded)
        {
            if (grounded) {
                StopAbility();
                // Remember the land time to prevent flying more than the ReoccuranceDelay.
                m_LandTime = Time.time;
            }
        }
       
        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        protected override void AbilityStopped()
        {
            base.AbilityStopped();

            m_Flying = false;
            m_Rigidbody.useGravity = true;
            EventHandler.UnregisterEvent<bool>(m_GameObject, "OnControllerGrounded", OnGrounded);
            EventHandler.UnregisterEvent(m_GameObject, "OnAnimatorStartFly", OnStartFly);
        }
    }
}