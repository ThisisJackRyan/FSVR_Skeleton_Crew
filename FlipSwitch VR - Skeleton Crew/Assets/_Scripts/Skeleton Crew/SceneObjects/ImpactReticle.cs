using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ImpactReticle : NetworkBehaviour {

	public bool holdProjectileInAir = false;
	public GameObject particles,  spawnPos;
	public GameObject[] projectiles;
	[Tooltip("index = time left")]
	public Material[] countDownMaterials;
	public ParticleSystemRenderer skullParticleSystem;
	public float damageRadius = 5f;
	public int damage = 15;
	public GameObject[] deckDamagePrefabs;

	public void SetBall(GameObject newBall) {
		print("setting new ball to " + newBall);
		ball = newBall;
	}

	// Use this for initialization
	void Start() {
		StartCoroutine("CountDown");
		Invoke( "Kill", countDownMaterials.Length + 2 );

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
			//print( "here in loop" );
			if (i == 0) {
				if ( !holdProjectileInAir ) {
					SpawnBall();
				}

				//if (isServer) {
				//	RpcAssignBall( ball.GetComponent<NetworkIdentity>() );
				//}
				if (ball) {

					ball.GetComponent<Rigidbody>().isKinematic = false;
				}

				yield return null;
			}

			skullParticleSystem.material = countDownMaterials[i];
			yield return new WaitForSeconds(1);
		}
	}

	[ClientRpc]
	void RpcAssignBall( NetworkIdentity id ) {
		if ( isServer ) {
			return;
		}

		ball = ClientScene.FindLocalObject( id.netId );
	}

	void Kill() {
		if (!isServer) {
			return;
		}

		NetworkServer.Destroy( gameObject );
	}

	//[SyncVar]
	public GameObject ball;
	
	void SpawnBall() {
		if (!isServer) {
			return;
		}

		int RNG = Random.Range(0, projectiles.Length);
		ball = Instantiate(projectiles[RNG], spawnPos.transform.position, Quaternion.identity);
		//ball.name = "fuck this shit";
		//ClientScene.RegisterPrefab( ball );
		//ball.GetComponent<Rigidbody>().isKinematic = true;
		NetworkServer.Spawn(ball);
		StartCoroutine("SyncAfterFrame");
		//RpcAssignBall( ball.GetComponent<NetworkIdentity>() );
	}

	IEnumerator SyncAfterFrame() {
		yield return new WaitForEndOfFrame();
		if (isServer) {
			RpcAssignBall( ball.GetComponent<NetworkIdentity>() );
		}
		yield return new WaitForEndOfFrame();
		if (ball) {
			ball.GetComponent<ImpactProjectile>().SetReticle( gameObject );
		}

	}

	private void OnTriggerEnter( Collider other ) {
		if (other.gameObject == ball) {
			//other.GetComponent<SCProjectile>().KillProjectile();
			Explode();
		}
	}

	public override void OnNetworkDestroy() {
		CancelInvoke();
		StopAllCoroutines();
		base.OnNetworkDestroy();
	}

	public void OnDestroy() {
		CancelInvoke();
		StopAllCoroutines();
	}

	void Explode() {
		foreach ( ParticleSystem system in GetComponentsInChildren<ParticleSystem>()) {
			system.Clear(true);
			system.Stop(true);
		}

		if ( !isServer ) {
			return;
		}

		var boom = Instantiate( particles, transform.position, Quaternion.identity );
		NetworkServer.Spawn( boom );

		Collider[] hits = Physics.OverlapSphere( transform.position, damageRadius );
		for ( int i = 0; i < hits.Length; i++ ) {
			if ( hits[i].GetComponent<DamagedObject>() ) {
				hits[i].GetComponent<DamagedObject>().ChangeHealth( damage );
			} else if ( hits[i].GetComponentInParent<Player>() ) {
				hits[i].GetComponentInParent<Player>().ChangeHealth( damage );
			} else if ( hits[i].GetComponentInParent<Ratman>() ) {
				hits[i].GetComponentInParent<Ratman>().ChangeHealth( damage );
			}
		}

		NetworkServer.Destroy( gameObject );

		//spawn damage
		if (deckDamagePrefabs.Length > 0) {
			Collider[] cols = Physics.OverlapSphere( transform.position, 0.5f );
			foreach ( var item in cols ) {
				if (item.tag == "Indestructible") {
					return;
				}
			}

			int rng = Random.Range( 0, deckDamagePrefabs.Length );
			GameObject dmg = Instantiate( deckDamagePrefabs[rng], transform.position, Quaternion.identity );
			NetworkServer.Spawn( dmg );
		}
	}

	bool debug = false;
	private void OnDrawGizmos() {
		if (debug) {
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere( transform.position, damageRadius );
			Gizmos.color = Color.blue;

			Gizmos.DrawSphere( transform.position, 0.5f );
		}
	}



}

