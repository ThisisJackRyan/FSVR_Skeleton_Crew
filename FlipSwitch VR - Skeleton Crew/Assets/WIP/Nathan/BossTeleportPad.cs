using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BossTeleportPad : NetworkBehaviour {

	public EnemyCaptain captainReference;

	private bool isOccupied;

	private void OnTriggerEnter(Collider other) {
		if (!isServer || isOccupied) {
			print("trigger enter is returning");
			print("isServer is " + isServer);
			print("isOccupied is " + isOccupied);
			return;
		}

		if (other.GetComponent<EnemyTargetInit>()) {
			print("player stepped on pad. calling function on captain");
			captainReference.PlayerSteppedOnPad();
			isOccupied = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (!isServer || !isOccupied) {
			return;
		}

		if (other.GetComponent<EnemyTargetInit>()) {
			captainReference.PlayerSteppedOffPad();
			isOccupied = false;
		}
	}
}
