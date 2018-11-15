using UnityEngine;
using UnityEngine.Networking;
using HTC.UnityPlugin.Vive;
using System.Collections;
using UnityEngine.UI;
using System;
using Sirenix.OdinInspector;

public class FSVRPlayer : NetworkBehaviour {

	public GameObject[] objectsToAddToDict;
	public MonoBehaviour[] componetsToDisable;
	public GameObject[] objectsToDisable;

	public SteamVR_TrackedObject leftFoot, rightFoot, hip, leftHand, rightHand;

	public GameObject floatingScore;

	public Camera hostCamView, hostCamViewDepth;

	// Use this for initialization
	void Start () {
		print("fsvr player start. isLocalPlayer? " + isLocalPlayer);
		if ( isLocalPlayer ) {
			print("should be setting screen to black");
			SteamVR_Fade.Start( Color.black, 0 );
			GetComponent<Player>().TurnOffColliders();
			SetTrackerIDs();
			//GetComponentInChildren<SteamVR_PlayArea>().BuildMesh();
			print("post tracker set");
		} else {
			if (isServer) {
				//print("should be adding " + gameObject.name + " to host list");
				GameObject.FindObjectOfType<Host>().AddPlayerToHostList(gameObject);

				VariableHolder.instance.AddPlayerToScoreList( gameObject );
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
			print("player count true");
            //if (true) { 
			GetComponent<VRIKCalibrateOnStart>().CalibratePlayer();

			var iks = FindObjectsOfType<VRIKCalibrateOnStart>();
			//print(iks.Length);
			foreach (var item in iks) {
				//print(item.calibrated + " calibrated " + name);
				item.CalibratePlayer();
			}

			if (FindObjectOfType<Captain>()) {
				print("looking for captian");
				FindObjectOfType<CaptainDialogueLobby>().enabled = true;
				FindObjectOfType<Captain>().Init();
			}
            StartCoroutine("FadeIn");
		}

	}


	void OnLevelWasLoaded(int level) {
		if (isLocalPlayer) {
			if (level == 2) {
				SteamVR_Fade.Start(Color.clear, 1f);
			}
		}
	}

	private void SetPositionToZero() {
		transform.position = Vector3.zero;
	}

	IEnumerator FadeIn() {
		print("FADE IN");
		yield return new WaitForSecondsRealtime(1f);
		SteamVR_Fade.Start( Color.clear, 1 );
		if (FindObjectOfType<LobbyAudioPlayer>()) {
            FindObjectOfType<LobbyAudioPlayer>().PlayNewClip();
        }
        //transform.Find("You Can Move").gameObject.SetActive(true);
	}
	
	void SetTrackerIDs() {
		print("set tracker ids called. isServer? " + isServer);
		try {
			leftFoot.index = TrackerIds.leftFootId;
			rightFoot.index = TrackerIds.rightFootId;
			hip.index = TrackerIds.hipId;
			leftHand.index = (SteamVR_TrackedObject.EIndex)Controller.LeftController.index == SteamVR_TrackedObject.EIndex.None ? 0 : (SteamVR_TrackedObject.EIndex)Controller.LeftController.index;
			rightHand.index = (SteamVR_TrackedObject.EIndex)Controller.RightController.index == SteamVR_TrackedObject.EIndex.None ? 0 : (SteamVR_TrackedObject.EIndex)Controller.RightController.index;
		} catch (Exception e){
			Debug.LogError("trackers had a null ref, " + e.Message);
		}
    }

    public void SetCameraSettings(int playerNum, RenderTexture mirror) {
		switch (playerNum) {
			case 1:
				hostCamView.targetTexture = mirror;
				hostCamViewDepth.targetTexture = mirror;
				EnableCamera();
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
		hostCamViewDepth.enabled = true;
	}

	public void DisableCamera() {
		hostCamView.enabled = false;
		hostCamViewDepth.enabled = false;
	}

	public VariableHolder.PlayerScore.ScoreType type;

	[Button]
	public void GivePoints() {
		print("give points");
		if (!isServer) {
			print( "returning not server" );

			return;
		}
		VariableHolder.instance.IncreasePlayerScore( transform.root.gameObject, type, transform.position );
	}

	public void SpawnPointDisplay(Vector3 spawnPos, int value, GameObject player) {
		if (!isServer) {
			//print("not server");
			return;
		}

		RpcSpawnPointsOnLocalPlayer(spawnPos, value, player);		
	}

	[ClientRpc]
	private void RpcSpawnPointsOnLocalPlayer( Vector3 spawnPos, int value, GameObject player ) {
		if (isLocalPlayer && player == transform.root.gameObject ) {
			//print("local player and same player that scored");

			var g = Instantiate( floatingScore, spawnPos, Quaternion.identity );
			g.GetComponentInChildren<Text>().text = "+" + value;
		} else {
			//print("local player? " + isLocalPlayer + " , player is " + player.name + " , root is " + transform.root.name);
		}
	}
}
