using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabWeaponHand : MonoBehaviour {

	public bool isLeftHand;
	bool isLocal;
	GrabWeapon grabWeapon;

	private void Start() {
		isLocal = GetComponentInParent<ScriptSyncPlayer>().isLocalPlayer;
		grabWeapon = GetComponentInParent<GrabWeapon>();
	}

	private void OnTriggerStay( Collider other ) {
		if (!isLocal) {
			return;
		}

		grabWeapon.SendCommandToHighlight( isLeftHand );
	}

	private void OnTriggerExit( Collider other ) {
		if ( !isLocal ) {
			return;
		}

		grabWeapon.SendCommandToUnHighlight( isLeftHand );
	}

}
