using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ExitLobbySwitch : NetworkBehaviour {

	float timer = 0, timeToTrans = 2;
	bool active = false;
	public Transform spawnPos;
	public CaptainDialogueLobby captain;

	private void OnTriggerStay(Collider other) {
		if ( !isServer ) {
			return;
		}

		if (other.gameObject.GetComponentInParent<ChangeAvatar>() && active) {
			timer += Time.deltaTime;

			if (timer >= timeToTrans) {
				//if (ExitLobbyPlayerTrigger.playerDict.ContainsValue(false)) {
				//	//print("not enough players in the lobby trigger");
				//	foreach (var obj in ExitLobbyPlayerTrigger.playerDict) {
				//		//print(obj.Key.name + " has a value of " + obj.Value);
				//	}
				//	timeToTrans++;
				//} else {

					//if ( NumberOfPlayerHolder.instance.numberOfPlayers != ExitLobbyPlayerTrigger.playerDict.Count ) {
					//	//print( "all connected players are in place, but not all players are connected." );
					//	return;
					//}
					//transition
					//print("should be teleporting player");

					//StartCoroutine("FadeAndTeleport");
					StartFade();
					other.GetComponentInParent<Player>().TellCaptainToStartTutorial();
					
				//}
			}
		}
	}

	[Button]
	public void TeleportWorkAround() {
		if (!isServer) {
			return;
		}

		StartFade();
		FindObjectOfType<Player>().TellCaptainToStartTutorial();
	}

	public void StartFade() {
		//print( "asdfkj;asdf: SERVER pre call" );
		FindObjectOfType<LobbyDisabler>().TurnOffAfterDelay();

		RpcStartFade();
		//print( "asdfkj;asdf: SERVER" );
		StartCoroutine("FadeAndTeleport");
	}

	[ClientRpc]
	void RpcStartFade() {
		//print( "asdfkj;asdf: CLIENTS pre server check , client: " + isClient );

		if ( isServer ) {
			return;
		}
		//print( "asdfkj;asdf: CLIENTS" );

		StartCoroutine( "FadeAndTeleport" );
	}

	IEnumerator FadeAndTeleport() {
		SteamVR_Fade.Start(Color.black, 1f);
		yield return new WaitForSecondsRealtime(1.5f);
		foreach (var go in ExitLobbyPlayerTrigger.playerDict.Keys) {
			go.transform.root.position = spawnPos.position;
            go.transform.root.rotation = spawnPos.rotation;
			captain.gameObject.SetActive( false);
		}

		FindObjectOfType<GhostFreeRoamCamera>().transform.root.position = spawnPos.position;
		FindObjectOfType<LobbyDisabler>().TurnOffAfterDelay();
		SteamVR_Fade.Start(Color.clear, 2f);
	}

	private void OnTriggerEnter(Collider other) {
		if ( !isServer ) {
			return;
		}

		//print(other.name);
		if (other.gameObject.GetComponentInParent<ChangeAvatar>() && !active) {
			timer = 0;
			active = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if ( !isServer ) {
			return;
		}

		if (other.gameObject.GetComponentInParent<ChangeAvatar>() && active) {
			active = false;
		}
	}
}