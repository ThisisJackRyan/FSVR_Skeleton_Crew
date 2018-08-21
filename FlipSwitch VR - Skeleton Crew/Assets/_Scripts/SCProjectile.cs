using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCProjectile : MonoBehaviour {

	public int damage;
	public bool oneShotKill = false;

	// Use this for initialization//print(transform.position);
	void Awake() {
		Invoke("KillProjectile", 10f);
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

		KillProjectile();
	}

	private void KillProjectile() {
		Destroy(gameObject);
	}

}