using UnityEngine;
using UnityEngine.Networking;
using HTC.UnityPlugin.Vive;
using System.Collections;

public class FSVRPlayer : NetworkBehaviour {

	public GameObject[] objectsToAddToDict;
	public MonoBehaviour[] componetsToDisable;
	public GameObject[] objectsToDisable;

	public SteamVR_TrackedObject leftFoot, rightFoot, hip, leftHand, rightHand;

	public Camera hostCamView;

	// Use this for initialization
	void Start () {

		if ( isLocalPlayer ) {
			SetTrackerIDs();

			SteamVR_Fade.Start( Color.black, 0 );
			GetComponent<Player>().TurnOffColliders();

			//GetComponentInChildren<SteamVR_PlayArea>().BuildMesh();

		} else {
			if (isServer) {
				//print("should be adding " + gameObject.name + " to host list");
				GameObject.FindObjectOfType<Host>().AddPlayerToHostList(gameObject);
			} else {
				GetComponent<Player>().TurnOffColliders();
			}

			foreach (var com in componetsToDisable){
				com.enabled = false;
			}

			foreach (var obj in objectsToDisable)
			{
				obj.SetActive(false);
			}
			
		}


		foreach ( GameObject obj in objectsToAddToDict ) {
			ExitLobbyPlayerTrigger.playerDict.Add( obj, false );
		}

        if (NumberOfPlayerHolder.instance.numberOfPlayers == VariableHolder.instance.players.Count) {
            //if (true) { 
			GetComponent<VRIKCalibrateOnStart>().CalibratePlayer();

			var iks = FindObjectsOfType<VRIKCalibrateOnStart>();
			//print(iks.Length);
			foreach (var item in iks) {
				//print(item.calibrated + " calibrated " + name);
				item.CalibratePlayer();
			}

			FindObjectOfType<CaptainDialogueLobby>().enabled = true;
			FindObjectOfType<Captain>().Init();
			StartCoroutine("FadeIn");
		}

	}

	IEnumerator FadeIn() {
		yield return new WaitForSecondsRealtime(1f);
		SteamVR_Fade.Start( Color.clear, 1 );
		if (FindObjectOfType<LobbyAudioPlayer>()) {
            FindObjectOfType<LobbyAudioPlayer>().PlayNewClip();
        }
        //transform.Find("You Can Move").gameObject.SetActive(true);
	}
	
	void SetTrackerIDs() {
        print("set tracker ids called. isServer? " + isServer);
		leftFoot.index = TrackerIds.leftFootId;
		rightFoot.index = TrackerIds.rightFootId;
		hip.index = TrackerIds.hipId;
        //leftHand.index = (SteamVR_TrackedObject.EIndex)Controller.LeftController.index == SteamVR_TrackedObject.EIndex.None ? 0 : (SteamVR_TrackedObject.EIndex)Controller.LeftController.index;
        //rightHand.index = (SteamVR_TrackedObject.EIndex)Controller.RightController.index == SteamVR_TrackedObject.EIndex.None ? 0 : (SteamVR_TrackedObject.EIndex)Controller.RightController.index;
    }

    public void SetCameraSettings(int playerNum) {
		switch (playerNum) {
			case 1:
				hostCamView.rect = new Rect(new Vector2(0f, 0.55f), new Vector2(0.5f, 0.45f));
				break;
			case 2:
				hostCamView.rect = new Rect(new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.45f));
				break;
			case 3:
				hostCamView.rect = new Rect(new Vector2(0f, 0.10f), new Vector2(0.5f, 0.45f));
				break;
			case 4:
				hostCamView.rect = new Rect(new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.45f));
				break;
		}
	}

	public void EnableCamera() {
		hostCamView.enabled = true;
	}

	public void DisableCamera() {
		hostCamView.enabled = false;
	}
}
