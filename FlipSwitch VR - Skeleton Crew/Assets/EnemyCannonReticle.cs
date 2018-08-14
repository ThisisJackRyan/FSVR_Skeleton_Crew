using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyCannonReticle : NetworkBehaviour {

	public GameObject particles, reticle;
	public float explosionTimer = 5f, destroyTimer = 3f, damageRadius = 5f;
	public int damage = 15;

	// Use this for initialization
	void Start() {
		Invoke("Explode", explosionTimer );
		//todo still needs to be spawned by enemy cannon
		//todo may need to add in cannon ball spawning
	}

	// Update is called once per frame
	void Explode() {
		particles.SetActive(true);
		reticle.SetActive(false);

		if (!isServer) {
			return;
		}

		Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
		for (int i = 0; i < hits.Length; i++) {
			if (hits[i].GetComponent<DamagedObject>()) {
				hits[i].GetComponent<DamagedObject>().ChangeHealth(damage);
			}else if ( hits[i].GetComponent<Player>() ) {
				hits[i].GetComponent<ScriptSyncPlayer>().ChangeHealth( damage); 
			}
		}

		Invoke("DestroySelf", destroyTimer );
	}

	void DestroySelf() {
		Destroy(gameObject);
	}
}