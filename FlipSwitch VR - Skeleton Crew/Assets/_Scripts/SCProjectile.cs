using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class SCProjectile : NetworkBehaviour {

	public int damage;
	public bool oneShotKill = false, isDestructible = false;
	[HideInInspector]
	public GameObject reticle;

	// Use this for initialization//print(transform.position);
	void Awake() {
		Invoke("KillProjectile", 10f);
		//print("reticle is " + reticle);
		//print(name +  " is kinematic: " + GetComponent<Rigidbody>().isKinematic );
	}

	public void SetReticle(GameObject ret) {
		reticle = ret;
	}

	private void OnTriggerEnter(Collider other) {
		//print("projectile triggered by : " + other.gameObject.name);
		if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
			return;
		}

		if (oneShotKill) {
			if (other.tag == "Player") {
				other.GetComponent<ScriptSyncPlayer>().KillMe();
			} else if (other.tag == "Enemy") {
				other.GetComponent<Enemy>().KillMe();
			}
		}

		if ( other.tag == "BulletPlayer" ) {
			//destroy gameobject
			//print("in the bullet player if");
			HitByMusket( other.gameObject );
		} else if(!other.GetComponent<ImpactReticle>()) {

			KillProjectile();

		}

	}

	private void KillProjectile() {
		Destroy(gameObject);
		if (reticle && isServer) {
			NetworkServer.Destroy( reticle );
		}
	}

	public int health = 1;

	void HitByMusket( GameObject bullet ) {
		Destroy( bullet );

		health--;
		if ( health <= 0 ) {
			//print("called rpc bullet");
			NetworkServer.Destroy(gameObject);
			NetworkServer.Destroy( reticle );

		}
	}

}