

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Weapon : NetworkBehaviour {

	public WeaponData data;
	public Transform projectileSpawnPos;	
	public Transform fire;
	public CannonInteraction owningPlayerCannonScript;
	public Outline myOutline;

    public bool isBeingHeldByPlayer = false;
    public bool isBeingHeldByHolster = false;


    private void Start() {
		if(fire != null ) {
			print("called");
			fire.gameObject.SetActive( false );
		}

		myOutline = GetComponent<Outline>();
		myOutline.enabled = false;
	}

	public void SpawnBullet() {
		var bullet = Instantiate( data.projectile, projectileSpawnPos.position, Quaternion.identity );
		bullet.GetComponent<Rigidbody>().AddForce( projectileSpawnPos.forward * data.power, ForceMode.Impulse );
		bullet.GetComponent<SCProjectile>().damage = data.damage;
		Instantiate( data.particles, projectileSpawnPos.position, Quaternion.Euler( transform.forward ));
		GetComponent<AudioSource>().clip = data.firesound;
		GetComponent<AudioSource>().Play();
	}

	public void ToggleFire() {
		if ( fire.gameObject.activeInHierarchy ) {
			fire.gameObject.SetActive( false );
		} else {
			fire.gameObject.SetActive( true );
		}
	}

	public void TurnOffFire() {
		if (!fire) {
			return;
		}

		if ( fire.gameObject.activeInHierarchy ) {
			fire.gameObject.SetActive( false );
		}
	}

	private void OnDrawGizmos() {
		if ( projectileSpawnPos ) {
			Gizmos.DrawLine( projectileSpawnPos.transform.position, projectileSpawnPos.transform.position + projectileSpawnPos.transform.forward * .5f );
		}
	}
}

