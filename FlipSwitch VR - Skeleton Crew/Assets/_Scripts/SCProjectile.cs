﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class SCProjectile : NetworkBehaviour {

	public int damage;
	public bool oneShotKill = false, isDestructible = false;
	public GameObject deathParticles;
	public GameObject particles;
	public float particleKillTimer = 2f;
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
			if ( other.tag == "PlayerCollider" ) {
				other.GetComponentInParent<ScriptSyncPlayer>().KillMe();
			} else if ( other.tag == "Enemy" ) {
				other.GetComponent<Enemy>().KillMe();
			} else if (other.tag == "Ratman") {
				other.GetComponentInParent<Ratman>().KillMe();
			}
		}

		if ( other.tag == "BulletPlayer" ) {
			//destroy gameobject
			//print("in the bullet player if");
			HitByMusket( other.gameObject );
		}

		if (!other.GetComponent<ImpactReticle>()) {
			KillProjectile();
		}

	}

	public void KillProjectile() {
		if (!isServer) {
			return;
		}

		RpcKillProjectile();

		particles.transform.parent = null;
		Destroy(particles, particleKillTimer );
		Destroy(gameObject);
	}

	[ClientRpc]
	private void RpcKillProjectile() {
		if (isServer) {
			return;
		}

		particles.transform.parent = null;
		Destroy( particles, particleKillTimer );
		Destroy( gameObject );
	}

	public int health = 1;

	void HitByMusket( GameObject bullet ) {
		Destroy( bullet );

		if (!isServer) {
			return;
		}

		health--;
		if ( health <= 0 ) {
			//print("called rpc bullet");
			NetworkServer.Destroy(gameObject);
			NetworkServer.Destroy( reticle );
			if (deathParticles) {
				var dp = Instantiate(deathParticles, transform.position, Quaternion.identity);
				NetworkServer.Spawn(dp);
			}

		}
	}

}