using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabWeaponHand : MonoBehaviour {

	public bool isLeftHand;
	GrabWeapon grabWeapon;

	private void Start() {
		grabWeapon = GetComponentInParent<GrabWeapon>();
	}

	private void OnTriggerStay( Collider other ) {
        //print(name + " trigger stay");
		grabWeapon.SendCommandToHighlight( isLeftHand );
	}

	private void OnTriggerExit( Collider other ) {
        //print(name + " trigger exit");

        grabWeapon.SendCommandToUnHighlight( isLeftHand );
	}

}
