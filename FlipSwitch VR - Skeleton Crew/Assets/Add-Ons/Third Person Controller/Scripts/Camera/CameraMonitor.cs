using UnityEngine;
using Opsive.ThirdPersonController.Input;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// Monitor classes which must be added to the Camera GameObject. This class allows a custom camera controller class to be used without requiring the Third Person Controller
    /// Camera Controller and Camera Handler components.
    /// </summary>
    public class CameraMonitor : MonoBehaviour
    {
        [Tooltip("The distance to look ahead when aiming or determining an IK look position")]
        [SerializeField] protected float m_DefaultLookDistance = 15;
        [Tooltip("Prevent the look distance from going lower than the specified distance")]
        [SerializeField] protected float m_MinLookDistance = 4;

        // An enum of possible camera view modes.
        public enum CameraViewMode { ThirdPerson, TopDown, RPG, Pseudo3D }

        // Internal values
        private Ray m_TargetLookRay;
        private RaycastHit m_RaycastHit;
        private Transform m_Crosshairs;
        private float m_LookDistance;
        private Vector3 m_PrevMousePosition;
        private Plane m_HitPlane = new Plane();

        // SharedFields
        private SharedProperty<float> m_Recoil = null;
        private SharedProperty<CameraViewMode> m_ViewMode = null;
        private SharedProperty<Vector3> m_CameraOffset = null;
        private Ray SharedProperty_TargetLookRay { get { return m_TargetLookRay; } }
        private Transform SharedProperty_TargetLock { get { return TargetLock; } }
        private float SharedProperty_LookDistance { get { return m_LookDistance; } }

        // Exposed properties
        public GameObject Character { get { return m_Character; } set { InitializeCharacter(value); } }
        private float Recoil { get { return m_Recoil == null ? 0 : m_Recoil.Get(); } }
        public CameraViewMode ViewMode { get { return m_ViewMode == null ? CameraViewMode.ThirdPerson : m_ViewMode.Get(); } set { if (m_ViewMode != null) { m_ViewMode.Set(value); } } }
        private Vector3 CameraOffset { get { return m_CameraOffset == null ? Vector3.zero : m_CameraOffset.Get(); } }
        public Transform Crosshairs { set { m_Crosshairs = value; } }
        public Transform TargetLock { set { if (m_CameraController != null) m_CameraController.TargetLock = value; } get { return (m_CameraController != null) ? m_CameraController.TargetLock : null; } }
        private Vector2 CrosshairsLocation
        {
            get
            {
                var location = Vector2.zero;
                if (m_Crosshairs == null) {
                    location = Vector2.one / 2;
                } else {
                    var screenPoint = RectTransformUtility.WorldToScreenPoint(null, m_Crosshairs.position);
                    location.Set(screenPoint.x / m_Camera.pixelWidth, screenPoint.y / m_Camera.pixelHeight);
                }
                return location;
            }
        }

        // Component references
        private Camera m_Camera;
        private CameraController m_CameraController;
        private GameObject m_Character;
        private Transform m_CharacterTransform;
        private RigidbodyCharacterController m_CharacterController;
        private PlayerInput m_PlayerInput;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_Camera = GetComponent<Camera>();
            m_CameraController = GetComponent<CameraController>();

            SharedManager.Register(this);
        }

        /// <summary>
        /// Indicate which character the camera should monitor.
        /// </summary>
        /// <param name="character">The character to initialize. Can be null.</param>
        private void InitializeCharacter(GameObject character)
        {
            if (m_Character != null) {
                EventHandler.UnregisterEvent(m_Character, "OnRespawn", OnRespawn);
            }

            m_Character = character;
            // Don't call OnCameraAttackCharacter until after Start has been called.
            if (m_CameraOffset != null) {
                EventHandler.ExecuteEvent<GameObject>("OnCameraAttachCharacter", character);
            }

            if (m_Character == null) {
                m_CharacterTransform = null;
                m_CharacterController = null;
                m_PlayerInput = null;
                enabled = false;
                return;
            }

            m_CharacterTransform = character.transform;
            m_CharacterController = character.GetComponent<RigidbodyCharacterController>();
            m_PlayerInput = character.GetComponent<PlayerInput>();
            m_PrevMousePosition = Vector3.one * float.MaxValue;
            EventHandler.RegisterEvent(character, "OnRespawn", OnRespawn);
            enabled = true;
        }

        /// <summary>
        /// Initializes all of the SharedFields.
        /// </summary>
        private void Start()
        {
            SharedManager.InitializeSharedFields(gameObject, this);
            if (m_Character != null) {
                EventHandler.ExecuteEvent<GameObject>("OnCameraAttachCharacter", m_Character);
            }
        }

        /// <summary>
        /// Update the target look ray.
        /// </summary>
        private void FixedUpdate()
        {
            UpdateTargetLookRay(false);
        }

        /// <summary>
        /// Update the target look ray.
        /// </summary>
        private void LateUpdate()
        {
            UpdateTargetLookRay(true);
        }

        /// <summary>
        /// CharacterIK and Items will need to know the direction that the camera is looking. Determine the TargetRay ahead of time to reduce the number of times it needs to be computed.
        /// <param name="updateInput">Should the look ray be updated based on input? Will be false in FixedUpdate because the input should not be updated at a fixed rate.</param>
        /// </summary>
        private void UpdateTargetLookRay(bool updateInput)
        {
            var viewMode = ViewMode;
            if (viewMode == CameraViewMode.ThirdPerson || viewMode == CameraViewMode.RPG) {
                var screenPoint = Vector3.zero;
                screenPoint.x = Screen.width * CrosshairsLocation.x;
                screenPoint.y = Screen.height * CrosshairsLocation.y;
                m_TargetLookRay = m_Camera.ScreenPointToRay(screenPoint);
                if (Physics.Raycast(m_TargetLookRay, out m_RaycastHit, float.PositiveInfinity, LayerManager.Mask.IgnoreInvisibleLayersPlayerWater, QueryTriggerInteraction.Ignore)) {
                    m_LookDistance = Mathf.Max(m_RaycastHit.distance, m_MinLookDistance);
                } else {
                    m_LookDistance = m_DefaultLookDistance;
                }
            } else if (updateInput && (viewMode == CameraViewMode.TopDown || viewMode == CameraViewMode.Pseudo3D)) {
                m_TargetLookRay.origin = m_CharacterTransform.position + m_CharacterController.CapsuleCollider.center;
                if (m_CharacterController.LookInMoveDirection) {
                    if (ViewMode == CameraViewMode.TopDown) {
                        m_TargetLookRay.direction = m_CharacterTransform.forward;
                    } else { // Pseudo3D.
                        var direction = GetTopDownPsuedo3DDirection(viewMode);
                        // Prevent the character from aiming behind them while always looking in the move direction.
                        if (Vector3.Dot(direction, m_CharacterTransform.forward) < 0) {
                            direction.x = -direction.x;
                        }
                        m_TargetLookRay.direction = direction;
                    }
                } else {
                    m_TargetLookRay.direction = GetTopDownPsuedo3DDirection(viewMode);
                }
            }
        }

        /// <summary>
        /// Returns the Top Down or Pseudo3D direction.
        /// </summary>
        /// <param name="viewMode">The type of camera view.</param>
        /// <returns>The Top Down or Pseudo3D direction.</returns>
        private Vector3 GetTopDownPsuedo3DDirection(CameraViewMode viewMode)
        {
            var mousePosition = (Vector3)m_PlayerInput.GetMousePosition();
            if (viewMode == CameraViewMode.TopDown) {
                if ((mousePosition - m_PrevMousePosition).sqrMagnitude > 0.1f) {
                    m_PrevMousePosition = mousePosition;
                    var ray = m_Camera.ScreenPointToRay(mousePosition);
                    float distance;
                    m_HitPlane.SetNormalAndPosition(Vector3.up, m_TargetLookRay.origin);
                    if (m_HitPlane.Raycast(ray, out distance)) {
                        return (ray.GetPoint(distance) - m_TargetLookRay.origin).normalized;
                    }
                }
            }
            if ((mousePosition - m_PrevMousePosition).sqrMagnitude > 0.1f) {
                m_PrevMousePosition = mousePosition;
                var screenPoint = m_Camera.WorldToScreenPoint(m_TargetLookRay.origin);
                var direction = mousePosition - screenPoint;
                // Top down doesn't use the y direction.
                if (viewMode == CameraViewMode.TopDown) {
                    direction.z = direction.y;
                    direction.y = 0;
                }
                return direction;
            } else {
                var direction = Vector3.zero;
                direction.x = m_PlayerInput.GetAxis(Constants.YawInputName);
                // Top down doesn't use the y direction.
                if (viewMode == CameraViewMode.TopDown) {
                    direction.z = m_PlayerInput.GetAxis(Constants.PitchInputName);
                } else {
                    direction.y = m_PlayerInput.GetAxis(Constants.PitchInputName);
                }
                if (direction.sqrMagnitude > 0.1f) {
                    return direction.normalized ;
                }
            }
            return m_TargetLookRay.direction;
        }

        /// <summary>
        /// Returns the direction that the camera is looking.
        /// </summary>
        /// <param name="applyRecoil">Should the target ray take into account any recoil?</param>
        /// <returns>The direction that the camera is looking.</returns>
        public Vector3 SharedMethod_TargetLookDirection(bool applyRecoil)
        {
            return TargetLookDirection(m_TargetLookRay, TargetLock, applyRecoil ? Recoil : 0);
        }

        /// <summary>
        /// Returns the direction that the camera is looking.
        /// </summary>
        /// <param name="lookPoint">The reference point to compute the direction from.</param>
        /// <param name="raycastLookDistance">Should the raycast look distance be used?</param>
        /// <returns>The direction that the camera is looking.</returns>
        public Vector3 SharedMethod_TargetLookDirectionLookPoint(Vector3 lookPoint, bool raycastLookDistance)
        {
            return TargetLookDirection(m_TargetLookRay, lookPoint, TargetLock, Recoil, raycastLookDistance ? m_LookDistance : m_DefaultLookDistance, ViewMode);
        }

        /// <summary>
        /// Returns the point that the camera is looking at.
        /// </summary>
        /// <returns>The point that the camera is looking at.</returns>
        public Vector3 SharedMethod_TargetLookPosition()
        {
            return TargetLookPosition(m_TargetLookRay, TargetLock, m_DefaultLookDistance, ViewMode);
        }

        /// <summary>
        /// Returns the direction that the camera is looking.
        /// </summary>
        /// <param name="lookRay">The look ray of the camera.</param>
        /// <param name="targetLock">The transform to look at.</param>
        /// <param name="recoil">Any recoil that should be added to the direction.</param>
        /// <returns>The direction that the camera is looking with the added recoil.</returns>
        public static Vector3 TargetLookDirection(Ray lookRay, Transform targetLock, float recoil)
        {
            // Look at the target lock if there is one.
            if (targetLock != null) {
                return (targetLock.position - lookRay.origin).normalized;
            }

            // Account for any recoil
            if (recoil != 0) {
                var direction = lookRay.direction;
                direction.y += recoil;
                lookRay.direction = direction;
            }

            return lookRay.direction;
        }

        /// <summary>
        /// Returns the direction that the camera is looking.
        /// </summary>
        /// <param name="lookRay">The look ray of the camera.</param>
        /// <param name="lookPoint">The reference point to compute the direction from.</param>
        /// <param name="targetLock">The transform to look at.</param>
        /// <param name="recoil">The amount of recoil the direction should add.</param>
        /// <param name="distance">The distance that the character should look.</param>
        /// <param name="viewMode">The type of camera view.</param>
        /// <returns>The direction that the camera is looking.</returns>
        public static Vector3 TargetLookDirection(Ray lookRay, Vector3 lookPoint, Transform targetLock, float recoil, float distance, CameraViewMode viewMode)
        {
            // Look at the target lock if there is one.
            if (targetLock != null) {
                return (targetLock.position - lookPoint).normalized;
            }

            // Account for any recoil
            var direction = lookRay.direction;
            if (recoil != 0) {
                direction.y += recoil;
                lookRay.direction = direction;
            }

            if (viewMode == CameraViewMode.ThirdPerson || viewMode == CameraViewMode.RPG || viewMode == CameraViewMode.Pseudo3D) {
                return (lookRay.GetPoint(distance == -1 ? 1000 : distance) - lookPoint).normalized;
            }
            return direction;
        }

        /// <summary>
        /// Returns the point that the camera is looking at.
        /// </summary>
        /// <param name="lookRay">The look ray of the camera.</param>
        /// <param name="targetLock">The transform to look at.</param>
        /// <param name="distance">The distance that the character should look.</param>
        /// <param name="viewMode">The type of camera view.</param>
        /// <returns>The point that the camera is looking at.</returns>
        public static Vector3 TargetLookPosition(Ray lookRay, Transform targetLock, float distance, CameraViewMode viewMode)
        {
            // Look at the target lock if there is one.
            if (targetLock != null) {
                return targetLock.position;
            }

            return lookRay.GetPoint(distance == -1 ? 1000 : distance);
        }

        /// <summary>
        /// The character has respawned. Update the look ray.
        /// </summary>
        private void OnRespawn()
        {
            UpdateTargetLookRay(true);
        }
    }
}