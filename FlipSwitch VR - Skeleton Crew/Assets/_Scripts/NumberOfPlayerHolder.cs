using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class NumberOfPlayerHolder : NetworkBehaviour {

	public static NumberOfPlayerHolder instance;
	[SyncVar( hook = "OnNumPlayerChanged" )] public int numberOfPlayers;

	public void OnNumPlayerChanged(int n ) {
		numberOfPlayers = n;
	}


	private void Awake() {
		if ( instance != null )
			Destroy( gameObject );
		else
			instance = this;
	}

	// Use this for initialization
	public override void OnStartServer () {
		base.OnStartServer();
		numberOfPlayers = VariableHolder.instance.numPlayers;
	}

	public override void OnStartClient() {
		base.OnStartClient();
		OnNumPlayerChanged( numberOfPlayers );
	}
}
