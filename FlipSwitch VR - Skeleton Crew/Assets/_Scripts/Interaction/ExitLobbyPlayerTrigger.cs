using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitLobbyPlayerTrigger : MonoBehaviour {

	public static Dictionary<GameObject, bool> playerDict = new Dictionary<GameObject, bool>();

	public string mainLevel;
	public GameObject[] exitSwitches;

	private void Start() {
		foreach ( var go in exitSwitches ) {
			go.SetActive( false );
		}
			EnableSwitches();
	}

	void EnableSwitches() {
		foreach (var go in exitSwitches) {
			go.SetActive( true );
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if (playerDict.ContainsKey(other.gameObject)) {
			playerDict[other.gameObject] = true;
		}

	}

	private void OnTriggerExit( Collider other ) {
		if ( playerDict.ContainsKey( other.gameObject ) ) {
			playerDict[other.gameObject] = false;
		}
	}
}
