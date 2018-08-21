using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    /// <summary>
    /// The AIAgent component allows Behavior Designer to tell the agent what point to look at. An example of where this is used is by the CharacterIK component so the 
    /// agent will aim correctly.
    /// </summary>
    public class AIAgent : MonoBehaviour
    {
        [Tooltip("Should the behavior tree be disabled when the character dies? It will be enabled again if the character respawns")]
        [SerializeField] private bool m_DisableTreeOnDeath = true;

        // Exposed properties
        public GameObject Target { get { return m_TargetTransform != null ? m_TargetTransform.gameObject : null; } set { m_TargetTransform = (value != null ? value.transform : null); } }

        // Component references
        [System.NonSerialized] protected GameObject m_GameObject;
        protected Transform m_Transform;
        protected Transform m_TargetTransform;
        private BehaviorTree m_BehaviorTree;

        /// <summary>
        /// Cache the component references and register with the SharedManager.
        /// </summary>
        protected virtual void Awake()
        {
            m_GameObject = gameObject;
            m_Transform = transform;
            if (m_DisableTreeOnDeath) {
                m_BehaviorTree = GetComponent<BehaviorTree>();
            }
            SharedManager.Register(this);
        }

        /// <summary>
        /// Register for any events that the agent should be made aware of.
        /// </summary>
        private void OnEnable()
        {
            if (m_DisableTreeOnDeath) {
                EventHandler.RegisterEvent(m_GameObject, "OnDeath", OnDeath);
            }
        }

        /// <summary>
        /// Unregister for any events that the agent should be made aware of.
        /// </summary>
        private void OnDisable()
        {
            if (m_DisableTreeOnDeath) {
                EventHandler.UnregisterEvent(m_GameObject, "OnDeath", OnDeath);
                EventHandler.UnregisterEvent(m_GameObject, "OnRespawn", OnRespawn);
            }
        }

        /// <summary>
        /// Returns the direction that the agent is looking.
        /// </summary>
        /// <param name="applyRecoil">Should the target ray take into account any recoil?</param>
        /// <returns>The direction that the agent is looking.</returns>
        public Vector3 SharedMethod_TargetLookDirection(bool applyRecoil)
        {
            // If the transform is null then return the forward direction.
            if (m_TargetTransform == null) {
                return m_Transform.forward;
            }

            return (m_TargetTransform.position - m_Transform.position).normalized;
        }

        /// <summary>
        /// Returns the direction that the agent is looking.
        /// </summary>
        /// <param name="lookPoint">The reference point to compute the direction from.</param>
        /// <param name="raycastLookDistance">Should the raycast look distance be used?</param>
        /// <returns>The direction that the agent is looking.</returns>
        public Vector3 SharedMethod_TargetLookDirectionLookPoint(Vector3 lookPoint, bool raycastLookDistance)
        {
            // If the transform is null then return the forward direction.
            if (m_TargetTransform == null) {
                return m_Transform.forward;
            }

            return (m_TargetTransform.position - lookPoint).normalized;
        }

        /// <summary>
        /// Returns the point that the agent is looking at.
        /// </summary>
        /// <returns>The point that the agent is looking at.</returns>
        public Vector3 SharedMethod_TargetLookPosition()
        {
            if (m_TargetTransform == null) {
                return m_Transform.TransformPoint(0, 0, 10);
            }
            return m_TargetTransform.position;
        }

        /// <summary>
        /// The character has died. Disable the behavior tree.
        /// </summary>
        private void OnDeath()
        {
            m_BehaviorTree.DisableBehavior();
            EventHandler.UnregisterEvent(m_GameObject, "OnDeath", OnDeath);
            EventHandler.RegisterEvent(m_GameObject, "OnRespawn", OnRespawn);
        }

        /// <summary>
        /// The character has respawned. Enable the behavior tree.
        /// </summary>
        private void OnRespawn()
        {
            m_BehaviorTree.EnableBehavior();
            EventHandler.UnregisterEvent(m_GameObject, "OnRespawn", OnRespawn);
            EventHandler.RegisterEvent(m_GameObject, "OnDeath", OnDeath);
        }
    }
}