using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyDisabler : MonoBehaviour {

	public GameObject[] additionalObjectsToDisable;

	// Use this for initialization
	public void TurnOffAfterDelay () {
		Invoke( "TurnOff", 5 );
	}

	void TurnOff() {
		foreach ( var obj in additionalObjectsToDisable) {
			Destroy( obj );
		}

		Destroy( gameObject );
		//gameObject.SetActive( false );
	}
}
