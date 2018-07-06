using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCProjectile : MonoBehaviour {

	public int damage;

	// Use this for initialization//print(transform.position);
	void Awake() {
		Invoke("KillProjectile", 10f);
	}

	private void OnCollisionEnter(Collision collision) {
		print("projectile collided with: " + collision.gameObject.name);
		if (collision.gameObject.tag == "Weapon" || collision.gameObject.tag == "Cannon") {
			return;
		}

		KillProjectile();
	}

	private void KillProjectile() {
		Destroy(gameObject);
	}

}