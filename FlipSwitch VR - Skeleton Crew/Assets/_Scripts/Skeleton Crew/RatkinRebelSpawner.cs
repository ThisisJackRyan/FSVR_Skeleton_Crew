using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class RatkinRebelSpawner : NetworkBehaviour {

    public GameObject ratkinRebelPrefab;
    public int spawnCountPerHatch = 2;

	// Use this for initialization
	void Start () {
        if (!isServer) {
            return;
        }

        HatchActivator.SpawnRatkinRebels(ratkinRebelPrefab, spawnCountPerHatch);
	}
}
