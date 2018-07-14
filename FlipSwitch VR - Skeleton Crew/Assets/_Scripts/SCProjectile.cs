using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCProjectile : MonoBehaviour {

	public int damage;

	// Use this for initialization//print(transform.position);
	void Awake() {
		Invoke("KillProjectile", 10f);
	}

	private void OnTriggerEnter(Collider other) {
		print("projectile triggered by : " + other.gameObject.name);
		if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon") {
			return;
		}

        if (other.tag == "Untagged") {
            print("Hit untagged");
        }

		KillProjectile();
	}

	private void KillProjectile() {
		Destroy(gameObject);
	}

}