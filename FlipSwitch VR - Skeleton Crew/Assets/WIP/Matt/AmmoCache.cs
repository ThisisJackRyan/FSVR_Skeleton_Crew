using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class AmmoCache : NetworkBehaviour{

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	float timer;
	bool active;
	GameObject activator;

	private void OnTriggerEnter( Collider other ) {
		if ( !isServer )
			return;

		if ( other.gameObject.GetComponent<Weapon>() && !active ) {
			timer = 0;
			active = true;
			activator = other.gameObject;
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

			if ( timer >= 1 ) {
				active = false;
				timer = 0;
				other.GetComponent < Weapon > ().Reload();
			}
		}
	}


}
