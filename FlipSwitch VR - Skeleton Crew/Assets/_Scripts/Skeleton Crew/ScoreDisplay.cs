using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ScoreDisplay : NetworkBehaviour {

	public Text display;
	
	// Update is called once per frame
	void Update () {
		if ( !isServer ) {
			return;
		}
		RpcUpdateDisplay( VariableHolder.instance.GetPlayerPoints( transform.root.gameObject ));
	}

	[ClientRpc]
	void RpcUpdateDisplay(string points) {
		if (isServer) {
			return;
		}
		display.text = points;
	}
}
