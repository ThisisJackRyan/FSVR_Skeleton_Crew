using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyCannonReticle : NetworkBehaviour {

	public bool holdProjectileInAir = false;
	public GameObject particles, projectile, spawnPos;
	[Tooltip("index = time left")]
	public Material[] countDownMaterials;
	public ParticleSystemRenderer skullParticleSystem;
	public float damageRadius = 5f;
	public int damage = 15;

	// Use this for initialization
	void Start() {
		StartCoroutine("CountDown");

		/*
		 start countdown
		 update graphic each second
		 on 0 enable RB on prjectile to drop
		 on enter explode and spawn particles		 
		 */
	}

	// Update is called once per frame

	IEnumerator CountDown() {
		if (holdProjectileInAir) {
			SpawnBall();
		}

		for (int i = countDownMaterials.Length - 1; i < countDownMaterials.Length && i >= 0; i--) {
			print( "here in loop" );
			if (i == 0) {
				if ( !holdProjectileInAir ) {
					SpawnBall();
				}
				ball.GetComponent<Rigidbody>().isKinematic = false;
				yield return null;
			}

			skullParticleSystem.material = countDownMaterials[i];
			yield return new WaitForSeconds(1);
		}
	}

	GameObject ball;
	void SpawnBall() {
		ball = Instantiate(projectile, spawnPos.transform.position, Quaternion.identity);
		ball.GetComponent<Rigidbody>().isKinematic = true;
		NetworkServer.Spawn(ball);
	}

	private void OnTriggerEnter( Collider other ) {
		if (other.gameObject == ball) {
			Explode();
		}
	}

	void Explode() {
		var boom = Instantiate( particles, transform.position, Quaternion.identity );
		NetworkServer.Spawn( boom );


		foreach ( ParticleSystem system in GetComponentsInChildren<ParticleSystem>()) {
			system.Clear(true);
			system.Stop(true);
		}

		if ( !isServer ) {
			return;
		}

		Collider[] hits = Physics.OverlapSphere( transform.position, damageRadius );
		for ( int i = 0; i < hits.Length; i++ ) {
			if ( hits[i].GetComponent<DamagedObject>() ) {
				hits[i].GetComponent<DamagedObject>().ChangeHealth( damage );
			} else if ( hits[i].GetComponent<Player>() ) {
				hits[i].GetComponent<ScriptSyncPlayer>().ChangeHealth( damage );
			}
		}

	}

}

