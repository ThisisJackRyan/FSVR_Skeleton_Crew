using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EncounterSpawner : NetworkBehaviour {

    internal void SpawnEncounter(GameObject prefabToSpawn, Transform spawnPos) {
        if (!isServer) {
            return;
        }

        var g = Instantiate(prefabToSpawn, spawnPos.position, Quaternion.identity);
        NetworkServer.Spawn(g);
    }
}
