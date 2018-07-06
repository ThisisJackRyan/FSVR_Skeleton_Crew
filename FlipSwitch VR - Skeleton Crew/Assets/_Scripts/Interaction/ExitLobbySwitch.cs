using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitLobbySwitch : MonoBehaviour {

	float timer = 0, timeToTrans = 2;
	bool active = false;
	public Transform spawnPos;
	public CaptainDialogueLobby captain;

	private void OnTriggerStay(Collider other) {
		if (other.gameObject.GetComponentInParent<ChangeAvatar>() && active) {
			timer += Time.deltaTime;

			if (timer >= timeToTrans) {
				if (ExitLobbyPlayerTrigger.playerDict.ContainsValue(false)) {
					print("not enough players in the lobby trigger");
					foreach (var obj in ExitLobbyPlayerTrigger.playerDict) {
						print(obj.Key.name + " has a value of " + obj.Value);
					}
					timeToTrans++;
				} else {

					if ( NumberOfPlayerHolder.instance.numberOfPlayers != Mathf.CeilToInt( ExitLobbyPlayerTrigger.playerDict.Count / 2 ) ) {
						print( "all connected players are in place, but not all players are connected." );
						return;
					}
					//transition
					print("should be teleporting player");

					StartCoroutine("FadeAndTeleport");
				}
			}
		}
	}

	public void StartFade() {
		StartCoroutine("FadeAndTeleport");
	}

	IEnumerator FadeAndTeleport() {
		SteamVR_Fade.Start(Color.black, 1f);
		yield return new WaitForSecondsRealtime(1.5f);
		foreach (var go in ExitLobbyPlayerTrigger.playerDict.Keys) {
			go.transform.root.position = spawnPos.position;
			captain.gameObject.SetActive( false);
		}

		FindObjectOfType<GhostFreeRoamCamera>().transform.root.position = spawnPos.position;

		SteamVR_Fade.Start(Color.clear, 2f);
	}

	private void OnTriggerEnter(Collider other) {
		print(other.name);
		if (other.gameObject.GetComponentInParent<ChangeAvatar>()) {
			timer = 0;
			active = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.gameObject.GetComponentInParent<ChangeAvatar>()) {
			active = false;
		}
	}
}