using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {

	public GameObject toSpawn;
	public static  EnemySpawner me;


	private void Start() {
		me = this;
	}

	public void Spawner() {
		InvokeRepeating("Spawn", 30, 30);
	}

	void Spawn() {
		Instantiate(toSpawn, transform.position, Quaternion.identity);
	}

	public static void StopSpawning() {
		me.CancelInvoke();
	}
}
