using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;

public class ImpactReticuleSpawner : NetworkBehaviour {

	public GameObject deckMesh;
	public GameObject reticlePrefab;
	public int totalSpawns = 6;
	int curSpawn;
	public float timeBeforeStart = 1.5f, timeBetweenSpawns = 5, checkRadius = 0.5f;
	Vector3 gizmo = Vector3.zero;
    public bool debug = false, killOnComplete = true;

	[Button]
	private Vector3 GeneratePoint() {
		Vector3 retVect = Vector3.zero;

		Bounds deckBounds = deckMesh.GetComponent<MeshRenderer>().bounds;

		bool found = false;

		do {
			found = false;

			float y = deckMesh.transform.position.y;
			float x = Random.Range( deckBounds.min.x + 1, deckBounds.max.x - 1 );
			float z = Random.Range( deckBounds.min.z + 1, deckBounds.max.z - 1 );
			retVect.Set( x, y + 0.04f, z );

			//RaycastHit hit;
			RaycastHit[] hits = Physics.SphereCastAll( retVect, checkRadius, Vector3.up );

			foreach ( var hit in hits ) {
				//print( "spherecast hit " + hit.transform.name );
				if ( hit.transform.tag == "Indestructible" ) {
					found = true;
					break;
				}
			}

		} while ( found );

		gizmo = retVect;

		return retVect;
	}

	private void Start() {
		print("impact spawner spawned");
		InvokeRepeating( "SpawnReticle", timeBeforeStart, timeBetweenSpawns );
	}

	public void SpawnReticle() {
		if (!isServer) {
			return;
		}

		GameObject reticle = Instantiate(reticlePrefab, GeneratePoint(), Quaternion.identity);
		NetworkServer.Spawn(reticle);
		curSpawn++;
        if (curSpawn >= totalSpawns ) {

            if (killOnComplete) {
                NetworkServer.Destroy(gameObject);
            } else {
                CancelInvoke();
            }

        }
	}

	private void OnDrawGizmos() {
		if (debug) {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere( gizmo, checkRadius );
		}
	}


}
