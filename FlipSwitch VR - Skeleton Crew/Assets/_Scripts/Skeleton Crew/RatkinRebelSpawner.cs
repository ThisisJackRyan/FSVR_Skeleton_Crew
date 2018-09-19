using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class RatkinRebelSpawner : NetworkBehaviour {

    public GameObject ratkinRebelPrefab;

	// Use this for initialization
	void Start () {
        if (!isServer) {
            return;
        }

        foreach(var s in GameObject.FindObjectOfType<PathFollower>().ratkinSpawnPositions) {
            var g = Instantiate(ratkinRebelPrefab, s.transform.position, Quaternion.identity);

            NetworkServer.Spawn(g);
        }
	}
}
