using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SCProjectile : NetworkBehaviour {

	public int damage;
    //public LayerMask ignoreMask;
	//public GameObject deathParticles;
	//public GameObject particles;
	public float particleKillTimer = 2f;

	// Use this for initialization//print(transform.position);
	void Awake() {
        Physics.IgnoreLayerCollision(0, 10);
        if (!isServer) {
            return;
        }

		Invoke("KillProjectile", 10f);
    }

    private void OnTriggerEnter(Collider other) {
        if (!isServer) {
            return;
        }


            if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
                return;
            }

            KillProjectile();
        
    }

    private void OnCollisionEnter(Collision other) {
        if (!isServer) {
            return;
        }

        if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
            return;
        }

        //print("collision enter on " + name);
        KillProjectile();
    }


    public void KillProjectile() {
        if (isServer) {
            RpcDestroy();
            Destroy(gameObject);
        }
	}

    public void RpcDestroy() {
        Destroy(gameObject);
    }
}