using System;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class EncounterNode : MonoBehaviour {
	public GameObject prefabToSpawn;
	[Tooltip("OPTIONAL; if not assigned will use this transform.")]
	public Transform spawnPos;

    internal void SpawnEncounter() {
        if (spawnPos == null) {
            spawnPos = transform;
        }

        GetComponentInParent<EncounterSpawner>().SpawnEncounter(prefabToSpawn, spawnPos);
    }
}