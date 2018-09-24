using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KillWeaponAfterTime : NetworkBehaviour {

	Dictionary<GameObject, float> enterTimes;
	public float timeToKill = 15;

	private void Start() {
		enterTimes = new Dictionary<GameObject, float>();
	}

	private void OnTriggerEnter( Collider other ) {
		if (other.tag != "Weapon") {
			return;
		}

		if (other.GetComponent<Weapon>().isBeingHeldByPlayer || other.GetComponent<Weapon>().playerWhoHolstered) {
			return;
		}

		if (!enterTimes.ContainsKey(other.gameObject)) {
			enterTimes.Add( other.gameObject, Time.time );
		} else {
			enterTimes[other.gameObject] = Time.time;
		}
	}

	private void OnTriggerExit( Collider other ) {
		if ( other.tag != "Weapon" ) {
			return;
		}

		if ( enterTimes.ContainsKey( other.gameObject ) ) {
			enterTimes.Remove( other.gameObject);
		}
	}

	private void OnTriggerStay( Collider other ) {
		if ( other.tag != "Weapon" ) {
			return;
		}

		if ( other.GetComponent<Weapon>().isBeingHeldByPlayer || other.GetComponent<Weapon>().playerWhoHolstered) {
			return;
		}

		if ( !enterTimes.ContainsKey( other.gameObject ) ) {
			Debug.LogWarning("Somehow " +  other.gameObject.name + " is tagged weapon, but was not added to the enter times dictionary.");
			return;
		} else {
			if((enterTimes[other.gameObject] + timeToKill) <= Time.time ) {
				NetworkServer.Destroy( other.transform.root.gameObject );
			}
		}
	}

}
