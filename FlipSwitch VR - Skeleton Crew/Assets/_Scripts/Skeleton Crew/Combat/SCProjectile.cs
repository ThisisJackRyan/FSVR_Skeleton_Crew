using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class SCProjectile : NetworkBehaviour {

	public int damage;
	public GameObject deathParticles;
	public GameObject particles;
	public float particleKillTimer = 2f;


	// Use this for initialization//print(transform.position);
	void Awake() {
		Invoke("KillProjectile", 10f);
    }

	

	private void OnTriggerEnter(Collider other) {
		//print("projectile triggered by : " + other.gameObject.name);
		if (other.gameObject.tag == "Weapon" || other.gameObject.tag == "Cannon" || other.gameObject.tag == "WeaponPickup") {
			return;
		}

		KillProjectile();
	}

    private void OnCollisionEnter(Collision collision) {
        if (!isServer) {
            return;
        }

        //print("collision enter on " + name);
        KillProjectile();
    }


    public void KillProjectile() {
        //if ( !isServer ) {
        //	return;
        //}

        RpcKillProjectile();

        //todo needs fixed
        if (particles) {
            particles.transform.parent = null;
			Destroy(particles, particleKillTimer );
		}

        //print("Should be destroying the bullet");
		NetworkServer.Destroy(gameObject);
	}

	[ClientRpc]
	private void RpcKillProjectile() {
		if (isServer) {
			return;
		}

		particles.transform.parent = null;
		Destroy( particles, particleKillTimer );
		//Destroy( gameObject );
	}


	

}