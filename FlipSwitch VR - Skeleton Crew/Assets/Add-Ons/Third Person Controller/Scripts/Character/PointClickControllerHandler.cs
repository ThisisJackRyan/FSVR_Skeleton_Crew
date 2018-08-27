using UnityEngine;
using UnityEngine.EventSystems;
using Opsive.ThirdPersonController.Input;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Allows the player to click to move the character to a position. Will translate the NavMeshAgent desired velocity into values that the RigidbodyCharacterController can understand.
    /// </summary>
    public class PointClickControllerHandler : NavMeshAgentBridge
    {
        // Internal variables
        private Vector3 m_Velocity;
        private Quaternion m_LookRotation;
        private bool m_AllowGameplayInput = true;

        // Component references
        private PlayerInput m_PlayerInput;
        private Camera m_Camera;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_PlayerInput = GetComponent<PlayerInput>();
            m_Camera = Utility.FindCamera(gameObject);

            SharedManager.Register(this);
        }

        /// <summary>
        /// Ensure the controller is set to the correct movement type and registered for any interested events.
        /// </summary>
        private void Start()
        {
            EventHandler.RegisterEvent<bool>(gameObject, "OnAllowGameplayInput", AllowGameplayInput);

#if UNITY_EDITOR || DLL_RELEASE
            // The controller must use the PointClick movement type with this component.
            if (GetComponent<RigidbodyCharacterController>().Movement != RigidbodyCharacterController.MovementType.PointClick) {
                Debug.LogWarning("Warning: The PointClickControllerHandler component has been started but the RigidbodyCharacterController is not using the PointClick movement type.");
            }
#endif
        }

        /// <summary>
        /// Move towards the mouse position if the MoveInput has been pressed. Translates the NavMeshAgent desired velocity into values that the RigidbodyCharacterController can understand.
        /// </summary>
        protected override void FixedUpdate()
        {
           if(m_AllowGameplayInput && m_PlayerInput.GetButton(Constants.MoveInputName) && !EventSystem.current.IsPointerOverGameObject()) {
                RaycastHit hit;
                // Fire a raycast in the direction that the camera is looking. Move to the hit point if the raycast hits the ground.
                if (Physics.Raycast(m_Camera.ScreenPointToRay(UnityEngine.Input.mousePosition), out hit, Mathf.Infinity, LayerManager.Mask.Ground, QueryTriggerInteraction.Ignore)) {
                    if (hit.transform.gameObject.layer != LayerManager.Enemy) {
                        m_NavMeshAgent.SetDestination(hit.point);
                    }
                }
            }

            base.FixedUpdate();
        }

        /// <summary>
        /// Is gameplay input allowed? An example of when it will not be allowed is when there is a fullscreen UI over the main camera.
        /// </summary>
        /// <param name="allow">True if gameplay is allowed.</param>
        private void AllowGameplayInput(bool allow)
        {
            m_AllowGameplayInput = allow;
        }
    }
}