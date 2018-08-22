using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ImpactReticuleSpawner : NetworkBehaviour {

    public GameObject deckMesh;
    public GameObject reticlePrefab;
    public float minHeight, maxHeight;

	private Vector3 GeneratePoint() {
        Vector3 retVect = Vector3.zero;

        Bounds deckBounds = deckMesh.GetComponent<Mesh>().bounds;

        float x = Random.Range(deckBounds.min.x + 1, deckBounds.max.x - 1);
        float z = Random.Range(deckBounds.min.z + 1, deckBounds.max.z - 1);
        retVect.Set(x, 0, z);
        return retVect;
    }

    public void SpawnReticle() {
        if (!isServer) {
            return;
        }

        GameObject reticle = Instantiate(reticlePrefab, GeneratePoint(), Quaternion.identity);
        NetworkServer.Spawn(reticle);
    }
}
