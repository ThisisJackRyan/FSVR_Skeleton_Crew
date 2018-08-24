using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class DieAfterTimeNetworked : NetworkBehaviour {

	public float particleLifetime;

	// Use this for initialization
	void Start() {
		//Debug.Break();
		Invoke( "Die", 5f );
	}

	// Update is called once per frame
	void Die() {
		if (!isServer) {
			return;
		}
		NetworkServer.Destroy( gameObject );
	}
}