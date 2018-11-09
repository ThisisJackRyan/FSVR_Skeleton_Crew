using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPatternNode : MonoBehaviour {

	public RepairPattern pattern;
	public GameObject repairSphere;
	bool active;

	private void OnEnable() {
		//pattern = GetComponentInParent<RepairPattern>();
		//print(name +  " enabled" );

		Invoke( "Timer", 5 );
	}

	void Timer() {
		//print(name + " timer ran out");
		pattern.gameObject.SetActive( false );
		if ( repairSphere ) {
			//repairSphere.GetComponent<Renderer>().enabled = true;
			repairSphere.GetComponent<RepairTrigger>().repairPattern = null;
			repairSphere.GetComponent<RepairTrigger>().particles.SetActive(true);
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( !other.GetComponentInParent<MastInteraction>() ) {
			return;
		}

		//Controller.PlayHaptics(other.gameObject.GetComponent<GrabWeaponHand>().isLeftHand, HapticController.BurstHaptics);

		pattern.Increment(other.transform.root.gameObject);
		CancelInvoke();
		gameObject.SetActive( false );
	}

	private void OnDisable() {
		//print( name + " canceling invoke on disable, pattern is " + pattern.name );
		CancelInvoke();
	}
}
