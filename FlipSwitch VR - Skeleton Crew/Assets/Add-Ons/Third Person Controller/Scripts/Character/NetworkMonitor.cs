using UnityEngine;
using UnityEngine.Networking;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// The NetworkMonitor acts as an intermediary component between the network and any object related to the character that is not spawned. These objects do not have
    /// the NetworkIdentifier component so they cannot issue standard RPC or Command calls. It also performs various other network functions such as contain the NetworkMessage identifiers.
    /// </summary>
    public class NetworkMonitor : NetworkBehaviour
    {
        // Internal variables
        private SharedMethod<int, GameObject> m_GameObjectWithItemID = null;
        private SharedProperty<Ray> m_TargetLookRay = null;
        private SharedProperty<Transform> m_TargetLock = null;
        private SharedProperty<float> m_LookDistance = null;
        private SharedProperty<float> m_Recoil = null;
        private SharedProperty<CameraMonitor.CameraViewMode> m_ViewMode = null;

        private Ray m_CameraTargetLookRay = new Ray();
        private Transform m_CameraTargetLock;
        private float m_CameraLookDistance;
        private float m_CameraRecoil;
        private float m_LastSyncTime = -1;

        // Component references
        private NetworkTransform m_NetworkTransform;

        /// <summary>
        /// Cache the component references and initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_NetworkTransform = GetComponent<NetworkTransform>();

            SharedManager.Register(this);
        }

        /// <summary>
        /// Initializes all of the SharedFields.
        /// </summary>
        private void Start()
        {
            SharedManager.InitializeSharedFields(gameObject, this);

            // The NetworkMonitor only needs to update for the camera. There is no camera for a non-local player so disable the component if not local.
            if (isLocalPlayer) {
                var camera = Utility.FindCamera(gameObject);
                SharedManager.InitializeSharedFields(camera.gameObject, this);
                camera.GetComponent<CameraController>().Character = gameObject;
            } else {
                enabled = false;
            }
        }

        /// <summary>
        /// Update the look variables of the camera. This is sent to all of the other clients so the clients can accurately update the character's IK.
        /// </summary>
        private void Update()
        {
            var lookRay = m_TargetLookRay.Get();
            // The server only needs to know the look position when the character is aiming. Don't sync every frame because that would cause too much bandwidth.
            if (m_LastSyncTime + m_NetworkTransform.GetNetworkSendInterval() < Time.time) {
                CmdUpdateCameraVariables(lookRay.origin, lookRay.direction, m_LookDistance.Get(), m_Recoil.Get());
                var targetLock = m_TargetLock.Get();
                if (m_CameraTargetLock != targetLock) {
                    CmdUpdateCameraTargetLock(targetLock != null ? targetLock.gameObject : null);
                    m_CameraTargetLock = targetLock;
                }
                m_LastSyncTime = Time.time;
            }
            // Update the camera variables immediately on the local machine. There is no reason to wait for the server to update these variables.
            UpdateCameraVariables(lookRay.origin, lookRay.direction, m_LookDistance.Get(), m_Recoil.Get());
        }

        /// <summary>
        /// Tell the server the camera variables of the local player.
        /// </summary>
        /// <param name="origin">The origin of the camera's look ray.</param>
        /// <param name="direction">The direction of hte camera's look ray.</param>
        /// <param name="recoil">Any recoil that should be added.</param>
        [Command(channel=(int)QosType.Unreliable)]
        private void CmdUpdateCameraVariables(Vector3 origin, Vector3 direction, float lookDistance, float recoil)
        {
            UpdateCameraVariables(origin, direction, lookDistance, recoil);
            RpcUpdateCameraVariables(origin, direction, lookDistance, recoil);
        }

        /// <summary>
        /// Send all of the camera variables to the clients.
        /// </summary>
        /// <param name="origin">The origin of the camera's look ray.</param>
        /// <param name="direction">The direction of hte camera's look ray.</param>
        /// <param name="recoil">Any recoil that should be added.</param>
        [ClientRpc]
        private void RpcUpdateCameraVariables(Vector3 origin, Vector3 direction, float lookDistance, float recoil)
        {
            if (!isLocalPlayer) {
                UpdateCameraVariables(origin, direction, lookDistance, recoil);
            }
        }

        /// <summary>
        /// Tells the server the camera lock value.
        /// </summary>
        /// <param name="targetLock">The value of the camera lock.</param>
        [Command]
        private void CmdUpdateCameraTargetLock(GameObject targetLock)
        {
            RpcUpdateCameraTargetLock(targetLock);
        }

        /// <summary>
        /// Send the camera lock value to the clients.
        /// </summary>
        /// <param name="origin">The value of the camera lock.</param>
        private void RpcUpdateCameraTargetLock(GameObject targetLock)
        {
            if (!isLocalPlayer) {
                m_CameraTargetLock = targetLock.transform;
            }
        }

        /// <summary>
        /// Update the camera variables for the attached character.
        /// </summary>
        /// <param name="origin">The origin of the camera's look ray.</param>
        /// <param name="direction">The direction of hte camera's look ray.</param>
        /// <param name="recoil">Any recoil that should be added.</param>
        private void UpdateCameraVariables(Vector3 origin, Vector3 direction, float lookDistance, float recoil)
        {
            m_CameraTargetLookRay.origin = origin;
            m_CameraTargetLookRay.direction = direction;
            m_CameraLookDistance = lookDistance;
            m_CameraRecoil = recoil;
        }

        /// <summary>
        /// Returns the direction that the camera is looking. An example of where this is used include when the GUI needs to determine if the crosshairs is looking at any enemies.
        /// </summary>
        /// <param name="applyRecoil">Should the target ray take into account any recoil?</param>
        /// <returns>A ray in the direction that the camera is looking.</returns>
        private Vector3 SharedMethod_TargetLookDirection(bool applyRecoil)
        {
            return CameraMonitor.TargetLookDirection(m_CameraTargetLookRay, m_CameraTargetLock, applyRecoil ? m_CameraRecoil : 0);
        }

        /// <summary>
        /// Returns the direction that the camera is looking.
        /// </summary>
        /// <param name="lookPoint">The reference point to compute the direction from.</param>
        /// <param name="raycastLookDistance">Should the raycast look distance be used?</param>
        /// <returns>The direction that the camera is looking.</returns>
        public Vector3 SharedMethod_TargetLookDirectionLookPoint(Vector3 lookPoint, bool raycastLookDistance)
        {
            // The SharedMethod may be called before Start is called.
            if (m_ViewMode == null) {
                SharedManager.InitializeSharedFields(Utility.FindCamera(gameObject).gameObject, this);
            }

            return CameraMonitor.TargetLookDirection(m_CameraTargetLookRay, lookPoint, m_CameraTargetLock, m_CameraRecoil, m_CameraLookDistance, m_ViewMode.Get());
        }

        /// <summary>
        /// Returns the point that the canera is looking at.
        /// </summary>
        /// <returns>The point that the camera is looking at.</returns>
        public Vector3 SharedMethod_TargetLookPosition()
        {
            // The SharedMethod may be called before Start is called.
            if (m_ViewMode == null) {
                SharedManager.InitializeSharedFields(Utility.FindCamera(gameObject).gameObject, this);
            }

            return CameraMonitor.TargetLookPosition(m_CameraTargetLookRay, m_CameraTargetLock, m_CameraLookDistance, m_ViewMode.Get());
        }

        /// <summary>
        /// Execute an event on all of the clients. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        public void ExecuteItemEvent(int itemID, string eventName)
        {
            RpcExecuteItemEvent(itemID, eventName);
        }

        /// <summary>
        /// Execute an event on the client. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        [ClientRpc]
        private void RpcExecuteItemEvent(int itemID, string eventName)
        {
            EventHandler.ExecuteEvent(m_GameObjectWithItemID.Invoke(itemID), eventName);
        }

        /// <summary>
        /// Execute an event on all of the clients with one argument. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        public void ExecuteItemEvent(int itemID, string eventName, bool arg1)
        {
            RpcExecuteItemEventBool(itemID, eventName, arg1);
        }

        /// <summary>
        /// Execute an event on the client with one argument. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// Note: A new method name was used because of a current Unity bug (697809).
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        [ClientRpc]
        private void RpcExecuteItemEventBool(int itemID, string eventName, bool arg1)
        {
            EventHandler.ExecuteEvent(m_GameObjectWithItemID.Invoke(itemID), eventName, arg1);
        }

        /// <summary>
        /// Execute an event on all of the clients with one argument. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        public void ExecuteItemEvent(int itemID, string eventName, Vector3 arg1)
        {
            RpcExecuteItemEventVector3(itemID, eventName, arg1);
        }

        /// <summary>
        /// Execute an event on the client with one argument. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// Note: A new method name was used because of a current Unity bug (697809).
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        [ClientRpc]
        private void RpcExecuteItemEventVector3(int itemID, string eventName, Vector3 arg1)
        {
            EventHandler.ExecuteEvent(m_GameObjectWithItemID.Invoke(itemID), eventName, arg1);
        }

        /// <summary>
        /// Execute an event on all of the clients with two arguments. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        public void ExecuteItemEvent(int itemID, string eventName, Vector3 arg1, Vector3 arg2)
        {
            RpcExecuteItemEventTwoVector3(itemID, eventName, arg1, arg2);
        }

        /// <summary>
        /// Execute an event on the client with two arguments. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// Note: A new method name was used because of a current Unity bug (697809).
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        [ClientRpc]
        private void RpcExecuteItemEventTwoVector3(int itemID, string eventName, Vector3 arg1, Vector3 arg2)
        {
            EventHandler.ExecuteEvent(m_GameObjectWithItemID.Invoke(itemID), eventName, arg1, arg2);
        }

        /// <summary>
        /// Execute an event on all of the clients with three arguments. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        public void ExecuteItemEvent(int itemID, string eventName, GameObject arg1, Vector3 arg2, Vector3 arg3)
        {
#if UNITY_EDITOR || DLL_RELEASE
            if (arg1.GetComponent<NetworkIdentity>() == null) {
                Debug.LogError("Error: " + arg1 + " must have the NetworkIdentity component added to it.");
            }
#endif
            RpcExecuteItemEventGameObjectTwoVector3(itemID, eventName, arg1, arg2, arg3);
        }

        /// <summary>
        /// Execute an event on the client with three arguments. Items will call this method because the items do not have a NetworkIdentifier and cannot call Rpc methods.
        /// Note: A new method name was used because of a current Unity bug (697809).
        /// </summary>
        /// <param name="itemID">The id of the item executing the event.</param>
        /// <param name="eventName">The name of the event to be executed.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        [ClientRpc]
        private void RpcExecuteItemEventGameObjectTwoVector3(int itemID, string eventName, GameObject arg1, Vector3 arg2, Vector3 arg3)
        {
            EventHandler.ExecuteEvent<Transform, Vector3, Vector3>(m_GameObjectWithItemID.Invoke(itemID), eventName, arg1.transform, arg2, arg3);
        }
    }
}