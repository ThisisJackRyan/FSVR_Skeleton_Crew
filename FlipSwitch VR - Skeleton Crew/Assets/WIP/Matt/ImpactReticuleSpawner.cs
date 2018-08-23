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
	public float timeBeforeStart = 1.5f, timeBetweenSpawns = 5;
	Vector3 gizmo = Vector3.zero;
	public bool debug = false;

	[Button]
	private Vector3 GeneratePoint() {
        Vector3 retVect = Vector3.zero;

        Bounds deckBounds = deckMesh.GetComponent<MeshRenderer>().bounds;

		float y = deckMesh.transform.position.y;
        float x = Random.Range(deckBounds.min.x + 1, deckBounds.max.x - 1);
        float z = Random.Range(deckBounds.min.z + 1, deckBounds.max.z - 1);
        retVect.Set(x, y + 0.04f, z);
		gizmo = retVect;

        return retVect;
    }

	private void Start() {
		InvokeRepeating( "SpawnReticle", timeBeforeStart, timeBetweenSpawns );
	}

	public void SpawnReticle() {
        if (!isServer) {
            return;
        }

        GameObject reticle = Instantiate(reticlePrefab, GeneratePoint(), Quaternion.identity);
        NetworkServer.Spawn(reticle);
		curSpawn++;
		if(curSpawn >= totalSpawns ) {
			NetworkServer.Destroy( gameObject );
		}
    }

	private void OnDrawGizmos() {
		if (debug) {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere( gizmo, 0.5f );
		}
	}


}
