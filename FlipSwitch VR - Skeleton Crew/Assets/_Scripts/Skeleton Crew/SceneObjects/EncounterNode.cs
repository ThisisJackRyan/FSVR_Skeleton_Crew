using System;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class EncounterNode : MonoBehaviour {
	public GameObject rangedPrefabToSpawn;
    public GameObject meleePrefabToSpawn;
	[Tooltip("OPTIONAL; if not assigned will use this transform.")]
	public Transform spawnPos;

	internal void SpawnEncounter() {
        if (spawnPos == null) {
            spawnPos = transform;
        }
        if (NumberOfPlayerHolder.instance.numberOfPlayers <= 2) {
            if (VariableHolder.instance.numRangedUnits > 2) {
                int rand = Random.Range(0, 100);
                if (rand <= 49) {
                    GetComponentInParent<EncounterSpawner>().SpawnEncounter(rangedPrefabToSpawn, spawnPos);
                } else {
                    GetComponentInParent<EncounterSpawner>().SpawnEncounter(meleePrefabToSpawn, spawnPos);
                }
            } else {
                GetComponentInParent<EncounterSpawner>().SpawnEncounter(rangedPrefabToSpawn, spawnPos);
            }
        } else {
            GetComponentInParent<EncounterSpawner>().SpawnEncounter(rangedPrefabToSpawn, spawnPos);
        }
	}
}