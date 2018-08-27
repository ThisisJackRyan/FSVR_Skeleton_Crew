using UnityEngine;
#if ENABLE_MULTIPLAYER
using UnityEngine.Networking;
#endif

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// PushableObject and replaced by MoveableObject.
    /// </summary>
#if ENABLE_MULTIPLAYER
    public class PushableObject : NetworkBehaviour
#else
    public class PushableObject : MonoBehaviour
#endif
    {
        private void Awake()
        {
            Debug.LogWarning("The PushableObject has been deprecated. Please use the MoveableObject instead.");
        }
    }
}