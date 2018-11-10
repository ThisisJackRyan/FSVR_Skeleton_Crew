using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class RepairDeckTrigger : MonoBehaviour {

	public GameObject particles;
	[HideInInspector]
	public RepairDeckPattern repairPattern;
	public DeckDamage deckDmg;
	Transform activator;
	public GameObject burst;

	float timer = 0;
	bool active = false;

	private void OnTriggerStay( Collider other ) {
		if ( other.transform.root != activator || !active ) {
			return;
		}

		timer += Time.deltaTime;

		if ( timer >= 1 ) {
			repairPattern.gameObject.SetActive( true ); //
			repairPattern.Init(activator); //

			particles.SetActive( false );

			deckDmg.SpawnBurst(burst, transform.position);
			//Controller.PlayHaptics(other.gameObject.GetComponent<GrabWeaponHand>().isLeftHand, HapticController.BurstHaptics);



			Vector3 hipPos = activator.GetComponentInChildren<HipMarker>().transform.position;

            transform.LookAt(new Vector3( hipPos.x, transform.position.y, hipPos.z));
            transform.Rotate(0, 180, 0);
            //transform.rotation = Quaternion.Euler(-transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, -transform.rotation.eulerAngles.z);
            //print(transform.rotation.eulerAngles);

            transform.position = new Vector3(transform.position.x,
                                    other.transform.root.GetComponentInChildren<HipMarker>().transform.position.y,
                                    transform.position.z);

            deckDmg.EnablePatternOnClients(); // <-- Enables pattern & disables particles on clients

            active = false;
			activator = null;
		}
	}



	private void OnTriggerEnter( Collider other ) {
		if ( !deckDmg.isServer ) {
			return;
		}

		if ( other.gameObject.GetComponentInParent<MastInteraction>() ) { //player check
			if ((repairPattern != null && repairPattern.gameObject.activeInHierarchy) || other.gameObject.GetComponentInParent<Player>().activeTrailHand /* Check for if player has already started repairing something */) { //pattern is active
				//print("returning on repair sphere because pattern is either set and active or player is already repairing something");
				return;
			}

			timer = 0;
			active = true;
			//particles.SetActive(true);
			//tracePrompt.SetActive( false );

			repairPattern = deckDmg.SelectPattern();

			activator = other.transform.root;

			//repairPattern.gameObject.SetActive( false );//
		}
	}




	private void OnTriggerExit( Collider other ) {
		if ( other.transform.root != activator ) {
			return;
		}

		active = false;
	}

	private void OnEnable() {
		//print("repair sphere has been enabled. Should be setting the particles to active. Disabling all other children. Should effectively initialize the repairing.");
		for ( int i = 0; i < transform.childCount; i++ ) {
			if ( i == 0 ) {
				transform.GetChild( i ).gameObject.SetActive( true );
				continue;
			}
			transform.GetChild( i ).gameObject.SetActive( false );
		}
	}



}
