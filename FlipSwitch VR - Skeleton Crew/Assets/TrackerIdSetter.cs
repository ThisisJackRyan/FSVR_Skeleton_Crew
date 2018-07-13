using System.Collections;
using HTC.UnityPlugin.Vive;
using UnityEngine;

public class TrackerIdSetter : MonoBehaviour {

    public LayerMask setterMask;
    public float radius = 0.05f;

    public void SetTrackerId() {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, setterMask);

        if (hits.Length > 0) {
            Debug.LogWarning("left hits length of " + hits.Length);

            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].tag == "LeftFootSetter") {
                    TrackerIds.leftFootId = GetComponent<SteamVR_TrackedObject>().index;
                    Debug.Log(name + " sets left " + TrackerIds.leftFootId);
                } else if (hits[i].tag == "RightFootSetter") {
                    TrackerIds.rightFootId = GetComponent<SteamVR_TrackedObject>().index;
                    Debug.Log(name + " sets right " + TrackerIds.rightFootId);

                } else if (hits[i].tag == "HipSetter") {
                    TrackerIds.hipId = GetComponent<SteamVR_TrackedObject>().index;
                    Debug.Log(name + " sets hip " + TrackerIds.hipId);

                }
            }
        }

        Debug.LogWarning(name + "hits length <= 0");

    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, radius);
    }
}

public static class TrackerIds {
    public static SteamVR_TrackedObject.EIndex leftFootId, rightFootId, hipId;

    public static string AsString {
        get {
            return "left: " + leftFootId + " right: " + rightFootId + " hip:" + hipId;
        }
    }
}
