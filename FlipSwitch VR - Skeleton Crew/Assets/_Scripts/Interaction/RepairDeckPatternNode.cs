using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairDeckPatternNode : MonoBehaviour {

	public RepairDeckPattern pattern;
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
			repairSphere.GetComponent<RepairDeckTrigger>().repairPattern = null;
			repairSphere.GetComponent<RepairDeckTrigger>().particles.SetActive( true );
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( !other.GetComponentInParent<MastInteraction>() ) {
			return;
		}
		pattern.Increment(other.transform.root.gameObject);
		CancelInvoke();
		gameObject.SetActive( false );
	}

	private void OnDisable() {
		//print( name + " canceling invoke on disable, pattern is " + pattern.name );
		CancelInvoke();
	}
}
