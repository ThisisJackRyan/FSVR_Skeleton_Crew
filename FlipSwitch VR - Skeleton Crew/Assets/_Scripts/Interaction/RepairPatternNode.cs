using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairPatternNode : MonoBehaviour {

	RepairPattern pattern;
	public GameObject repairSphere;
	bool active;

	private void OnEnable() {
		pattern = GetComponentInParent<RepairPattern>();
		Invoke( "Timer", 5 );
	}

	void Timer() {
		pattern.gameObject.SetActive( false );
		if ( repairSphere ) {
			repairSphere.GetComponent<Renderer>().enabled = true;
		}
	}

	private void OnTriggerEnter( Collider other ) {
		print("werfweqrwer");
		pattern.Increment();
		CancelInvoke();
		gameObject.SetActive( false );
	}

	
}
