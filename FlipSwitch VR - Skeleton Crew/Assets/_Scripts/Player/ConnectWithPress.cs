using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;

//[RequireComponent(typeof(LoadSceneOnStart))]
public class ConnectWithPress : MonoBehaviour {

    // Use this for initialization
    void OnEnable() {
        if (NetworkHelper.GetLocalIPAddress().Equals(NetworkHelper.hostIpAddress))
        {
            gameObject.SetActive(false);
            return;
        }
        Controller.InitControllers();
		//DontDestroyOnLoad(gameObject);
	}

	// Update is called once per frame
	void Update() {
		if (NetworkHelper.GetLocalIPAddress().Equals(NetworkHelper.hostIpAddress)) {
			return;
		}

		if (Controller.RightController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger)) {
			StartCoroutine("FadeAndLoad");
			
		}
	}

	[Button( "loopholes" )]
	void Ha() {
		StartCoroutine( "FadeAndLoad" );
	}	

	IEnumerator FadeAndLoad() {
		SteamVR_Fade.Start( Color.black, 1 , true);
		//SteamVR.instance.compositor.FadeToColor( 0, 0, 0, 0, 1, true );
		yield return new WaitForSecondsRealtime(1f);
		//SteamVR_Fade.View( Color.black, 3f );
		//GetComponentInChildren<Camera>().enabled = false;
		NetworkManager.singleton.StartClient();
		yield return new WaitForSecondsRealtime( 1f );
		SteamVR_Fade.Start( Color.clear, 1, true );
		//GetComponentInChildren<Camera>().enabled = true;

		//Destroy(gameObject);
	}
}

