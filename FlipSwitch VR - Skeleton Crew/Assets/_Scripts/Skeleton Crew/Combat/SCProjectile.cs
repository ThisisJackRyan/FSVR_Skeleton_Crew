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
	[SyncVar]
    public GameObject playerWhoFired = null;
	public bool isCannonball = false;

	// Use this for initialization//print(transform.position);
	void Awake() {
        //print("awake. s: " + isServer + " lp:" + isLocalPlayer + " c:" + isClient  );

        //Physics.IgnoreLayerCollision(0, 10);
        if (!isServer) {
            return;
        }

		print(name + " spawned");

		//print("invoking");
		Invoke("KillProjectile", 5);
    }

    //private void OnTriggerEnter(Collider other) {
    //    if (!isServer) {
    //        return;
    //    }


    //    if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
    //        return;
    //    }

    //    KillProjectile();
        
    //}

    private void OnCollisionEnter(Collision other) {
        if (!isServer) {
            return;
        }

        if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
            return;
        }

		print("collision enter on " + name + " with " + other.collider.name);
		KillProjectile();
    }


    public void KillProjectile() {
        //print("kill called");
        if (isServer) {
            NetworkServer.Destroy(gameObject);
        }
	}

    [ClientRpc]
    public void RpcDestroy() {
        //print("rpc destroy called");
        Destroy(gameObject);
    }
}