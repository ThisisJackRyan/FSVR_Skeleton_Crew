using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyDisabler : MonoBehaviour {

	// Use this for initialization
	public void TurnOffAfterDelay () {
		Invoke( "TurnOff", 5 );
	}

	void TurnOff() {
		gameObject.SetActive( false );
	}
}
