using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using HTC.UnityPlugin.Vive;

//[RequireComponent(typeof(LoadSceneOnStart))]
public class ConnectWithPress : MonoBehaviour {

    public ViveRoleSetter left, right;

    public TrackerIdSetter[] setters;

	public GameObject standStill;

    // Use this for initialization
    void OnEnable() {
        //if (NetworkHelper.GetLocalIPAddress().Equals(NetworkHelper.hostIpAddress))
        //{
        //    gameObject.SetActive(false);
        //    return;
        //}
        Invoke("InitController", 0.5f);
        //DontDestroyOnLoad(gameObject);
    }


    void Update() {
        //if (NetworkHelper.GetLocalIPAddress().Equals(NetworkHelper.hostIpAddress)) {
        //    return;
        //}

        if (canInput) {
            if (Controller.RightController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger)) {
                foreach (var item in setters) {
                    item.SetTrackerId();
                }

                StartCoroutine("FadeAndLoad");
            }
        }
    }

    bool canInput = false;

    void InitController() {
        canInput = true;
        Controller.InitControllers(left.viveRole.GetDeviceIndex(), right.viveRole.GetDeviceIndex());

    }

    [Button("loopholes")]
    void Ha() {
        StartCoroutine("FadeAndLoad");
    }

    IEnumerator FadeAndLoad() {
        SteamVR_Fade.Start(Color.black, 1, true);
		standStill.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        //NetworkManager.singleton.networkAddress = NetworkManager.singleton.serverBindAddress;
        NetworkManager.singleton.StartClient();

        yield return new WaitForSecondsRealtime(1f);
        SteamVR_Fade.Start(Color.clear, 1, true);
    }
}

