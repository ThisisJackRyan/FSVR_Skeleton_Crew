using UnityEngine;
using UnityEngine.Networking;
using HTC.UnityPlugin.Vive;
using System.Collections;

public class PlayerSetup : NetworkBehaviour {

	public GameObject[] objectsToAddToDict;
    public MonoBehaviour[] componetsToDisable;
    public GameObject[] objectsToDisable;

    public SteamVR_TrackedObject leftFoot, rightFoot, hip;


    [SyncVar( hook = "OnPlayerNumChange")] int playerNum = 0;

	public override void OnStartLocalPlayer() {
		base.OnStartLocalPlayer();
               
		GhostFreeRoamCamera go = GameObject.FindObjectOfType<GhostFreeRoamCamera>();
		go.GetComponent<Camera>().enabled = false;
		go.enabled = false;
	}	

	// Use this for initialization
	void Start () {
		if ( isLocalPlayer ) {
			CmdSetPlayerNum(int.Parse( NetworkHelper.GetLocalIPAddress().Substring( NetworkHelper.GetLocalIPAddress().Length - 1 ) ) );
			gameObject.name = "Player " + playerNum;

            SetTrackerIDs();

            SteamVR_Fade.Start( Color.black, 0 );
			StartCoroutine("FadeIn");

		} else {
			OnPlayerNumChange( playerNum );
			gameObject.name = "Player " + playerNum;

            foreach (var com in componetsToDisable){
                com.enabled = false;
            }

            foreach (var obj in objectsToDisable)
            {
                obj.SetActive(false);
            }
        }

		foreach ( GameObject obj in objectsToAddToDict ) {
			print( "adding " + obj.name + " to the player dictionary" );
			ExitLobbyPlayerTrigger.playerDict.Add( obj, false );
		}

		GetComponent<VRIKCalibrateOnStart>().CalibratePlayer();

		var iks = FindObjectsOfType<VRIKCalibrateOnStart>();
		print(iks.Length);
		foreach (var item in iks) {
			print(item.calibrated + " calibrated " + name);
			item.CalibratePlayer();
		}

		if ( NumberOfPlayerHolder.instance.numberOfPlayers == VariableHolder.instance.players.Count ) {
			print( "all players joined" );
			//var iks = FindObjectsOfType<VRIKCalibrateOnStart>();
			//foreach ( var item in iks ) {
			//	item.CalibratePlayer();
			//}
			FindObjectOfType<CaptainDialogueLobby>().enabled = true;
		}

	}

	IEnumerator FadeIn() {
		//print("fade corou");
		yield return new WaitForSecondsRealtime(1f);
		SteamVR_Fade.Start( Color.clear, 1 );
	}


    void SetTrackerIDs() {
        leftFoot.index = TrackerIds.leftFootId;
        rightFoot.index = TrackerIds.rightFootId;
        hip.index = TrackerIds.hipId;
    }

	[Command]
    private void CmdSetPlayerNum(int n)
    {
        playerNum = n;
    }

	private void OnPlayerNumChange(int num ) {
		playerNum = num;
    }
}
