using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairTrigger : MonoBehaviour {

	public RepairPattern repairPattern;

	float timer = 0;
	bool active = false;
	Vector3 startingPos;

	private void Start() {
		startingPos = transform.position;
	}

	private void OnTriggerStay( Collider other ) {
		if (other.gameObject.GetComponentInParent<MastInteraction>() && active) {
			timer += Time.deltaTime;

			if (timer >= 1) {
				repairPattern.gameObject.SetActive( true );
				repairPattern.Init();
				active = false;

				GetComponent<Renderer>().enabled = false;
				transform.position = new Vector3( transform.position.x, other.transform.root.GetComponentInChildren<HipMarker>().transform.position.y, transform.position.z );

			}
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( other.gameObject.GetComponentInParent<MastInteraction>() ) {
			if (repairPattern.gameObject.activeInHierarchy) {
				return;
			}

			timer = 0;
			active = true;
			GetComponent<Renderer>().enabled = true;
			repairPattern.gameObject.SetActive( false );

		}
	}

	private void OnTriggerExit( Collider other ) {
		active = false;
	}

}
