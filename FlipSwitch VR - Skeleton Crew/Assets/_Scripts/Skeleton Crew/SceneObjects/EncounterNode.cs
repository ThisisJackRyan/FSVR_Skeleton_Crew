using System;
using UnityEngine;
using UnityEngine.Networking;


public class EncounterNode : NetworkBehaviour {
	public GameObject prefabToSpawn;
	[Tooltip("OPTIONAL; if not assigned will use this transform.")]
	public Transform spawnPos;

	internal void SpawnEncounter() {
		if (!isServer) {
			return;
		}

		if (spawnPos == null) {
			spawnPos = transform;
		}

		var g = Instantiate(prefabToSpawn, spawnPos.position, Quaternion.identity);
		NetworkServer.Spawn(g);
	}
}