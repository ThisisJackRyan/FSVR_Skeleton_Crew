using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EncounterNode : NetworkBehaviour {

	public GameObject prefabToSpawn;
	public float spawnRadiusMin, spawnRadiusMax;
	public int numToSpawnMin, numToSpawnMax;
	public float spawnDistFromRock = 2;
	public Transform shipTransform;

	public static GameObject[] Floaters
	{
		get
		{
			return floaters ?? ( floaters = GameObject.FindGameObjectsWithTag( "Floater" ) );
		}
	}

	static GameObject[] floaters;

	public void Spawn () {
		if ( !isServer ) {
			return;
		}

		print(name + " called spawn" + Time.time);

		int spawnCount = Random.Range( numToSpawnMin, numToSpawnMax + 1 );

		for (int i = 0; i <= spawnCount; i++ ) {
			//determine range
			//float spawnDist = Random.Range( spawnRadiusMin, spawnRadiusMax );

			//find rock
			List<GameObject> rocks = new List<GameObject>();

			foreach (GameObject go in Floaters) {
				float dist = Vector3.Distance( shipTransform.position, go.transform.position );
				if (dist > spawnRadiusMin && dist < spawnRadiusMax ) {
					rocks.Add( go );
				}
			}

			if (rocks.Count > 0) {
				int chosenOne = Random.Range(0, rocks.Count);
			//calc other side
				Vector3 spawnVector = rocks[chosenOne].transform.position - shipTransform.position;
				spawnVector = rocks[chosenOne].transform.position + ( spawnVector.normalized * spawnDistFromRock );
				//spawn
				RpcSpawnEnemy( spawnVector );
			}
		}
	}
#pragma warning disable 0219
	[ClientRpc]
	private void RpcSpawnEnemy(Vector3 spawnPos ) {

		print( "should be spawning an enemy" );
		GameObject g = Instantiate( prefabToSpawn, spawnPos, Quaternion.identity );		
	}
}
