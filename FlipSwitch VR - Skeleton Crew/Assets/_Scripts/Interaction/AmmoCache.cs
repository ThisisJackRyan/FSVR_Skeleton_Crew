using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class AmmoCache : NetworkBehaviour{

	float timer;
	bool active;
	GameObject activator;
	public AudioClip reloadClip;

	private void OnTriggerEnter( Collider other ) {
		if ( !isServer )
			return;

		if ( other.gameObject.GetComponent<Weapon>() ) { 
			if ( other.gameObject.GetComponent<Weapon>().data.type == WeaponData.WeaponType.Gun && !active ) {
				timer = 0;
				active = true;
				activator = other.gameObject;
			}
		}
	}

	private void OnTriggerExit( Collider other ) {
		if ( !isServer ) {
			return;
		}
		if (activator == null || other.gameObject != activator) {
			return;
		}

		active = false;
		activator = null;
		timer = 0;
	}

	private void OnTriggerStay( Collider other ) {
		if ( !isServer )
			return;

		if ( other.gameObject == activator && active ) {
			timer += Time.deltaTime;

			if ( timer >= 0.5f ) {
				active = false;
				timer = 0;
				other.GetComponent < Weapon > ().Reload();
				GetComponent<AudioSource>().PlayOneShot( reloadClip );
				RpcPlaySound();
			}
		}
	}

	[ClientRpc]
	void RpcPlaySound() {
		if (isServer) {
			return;
		}
		GetComponent<AudioSource>().PlayOneShot( reloadClip );

	}

}
