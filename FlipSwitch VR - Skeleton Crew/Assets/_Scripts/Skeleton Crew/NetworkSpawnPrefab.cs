using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkSpawnPrefab : NetworkBehaviour {

	public GameObject prefab;
	public Vector3 pos;

	[Button]
	public void Spawn() {
		if (!isServer) {
			return;
		}

		var g = Instantiate( prefab, pos, Quaternion.identity );
		NetworkServer.Spawn( g );
	}
}
