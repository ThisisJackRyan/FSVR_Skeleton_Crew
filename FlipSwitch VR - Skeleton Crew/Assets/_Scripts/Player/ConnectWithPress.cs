using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using Valve.VR;

//[RequireComponent(typeof(LoadSceneOnStart))]
public class ConnectWithPress : MonoBehaviour {

    //public ViveRoleSetter left, right;

    public TrackerIdSetter[] setters;

	public GameObject standStill;
	//public GameObject vrLoadLevel;

    // Use this for initialization
    public void EnableInput() {
        Invoke("CanInputIsTrue", 0.5f);
    }

    void CanInputIsTrue() {
        canInput = true;
    }

    void Update() {
        //if (NetworkHelper.GetLocalIPAddress().Equals(NetworkHelper.hostIpAddress)) {
        //    return;
        //}

        if (canInput) {
            //print("input enabled");
            if (Controller.RightController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var item in setters) {
                    if (item) {
                        item.SetTrackerId();
                    } else {
                        continue;
                    }
                }
				//FindObjectOfType<SteamVR_LoadLevel>().Trigger();

				StartCoroutine("FadeAndLoad");
            }
        }
    }

    bool canInput = false;

    //void InitController() {
    //    canInput = true;
    //    Controller.InitControllers(left.viveRole.GetDeviceIndex(), right.viveRole.GetDeviceIndex());

    //}

    [Button("loopholes")]
    void Ha() {
		//FindObjectOfType<SteamVR_LoadLevel>().Trigger();  //("Master_Online_new");

        StartCoroutine("FadeAndLoad");
    }

    IEnumerator FadeAndLoad() {
		//SteamVR_Overlay.instance.UpdateOverlay();
		//var compositor = OpenVR.Compositor;
        SteamVR_Fade.Start(Color.black, 1, true);
		//compositor.FadeToColor(1f, 255f, 255f, 255f, 1.0f, false);
		standStill.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        //NetworkManager.singleton.networkAddress = NetworkManager.singleton.serverBindAddress;
        NetworkManager.singleton.StartClient();

        //yield return new WaitForSecondsRealtime(1f);
    }
}

