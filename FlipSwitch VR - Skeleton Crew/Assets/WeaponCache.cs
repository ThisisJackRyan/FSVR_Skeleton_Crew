using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeaponCache : NetworkBehaviour {

	public Transform spawnPos;
	public float timeBetweenSpawns = 30;
	public int startingCount = 5;
	public GameObject[] toSpawn;
	public Collider collider;
	public float spawnRadius;

	int counter = 0;

	void Start() {
		float tempTime = timeBetweenSpawns / startingCount;
		InvokeRepeating("Spawn", tempTime, tempTime);
	}

	// Update is called once per frame
	void Spawn() {
		if (!isServer) {
			return;
		}

		print("spawn on server");

		counter++;
		if (counter >= startingCount) {
			CancelInvoke();
			InvokeRepeating("Spawn", timeBetweenSpawns, timeBetweenSpawns);
		}

		int weapons = 0;
		foreach (var col in Physics.OverlapSphere(collider.bounds.center, spawnRadius)) {
			if (col.tag == "Weapon") {
				weapons++;
			}
		}

		if (weapons >= startingCount) {
			print("spawn on server found too many weapons, returning");

			return;
		}

		int rng = Random.Range(0, toSpawn.Length);
		GameObject go = toSpawn[rng];

		GameObject spawned = Instantiate(go, spawnPos.position, spawnPos.rotation);

		//RpcSpawn(go);
		NetworkServer.Spawn(spawned);


	}

	[ClientRpc]
	void RpcSpawn(GameObject go) {
		if (isServer) {
			print("rpc spawn on server with " + go.name);
			return;
		}

		print("rpc spawn on client");

		Instantiate(go, spawnPos.position, Quaternion.identity);
	}

	private void OnDrawGizmosSelected() {
		Gizmos.DrawWireSphere(collider.bounds.center, spawnRadius);
	}

}