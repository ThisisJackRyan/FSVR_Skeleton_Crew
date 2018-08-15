using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairTrigger : MonoBehaviour {

	public RepairPattern[] repairPatterns;
	RepairPattern repairPattern;
	Transform activator;

	float timer = 0;
	bool active = false;

	private void OnTriggerStay( Collider other ) {
		if (other.transform.root == activator && active) {
			timer += Time.deltaTime;

			if (timer >= 1) {
				repairPattern.gameObject.SetActive( true );//
				repairPattern.Init();//
				active = false;

				GetComponent<Renderer>().enabled = false;
				transform.position = new Vector3( transform.position.x, other.transform.root.GetComponentInChildren<HipMarker>().transform.position.y, transform.position.z );
			}
		}
	}

	private void OnTriggerEnter( Collider other ) {
		if ( other.gameObject.GetComponentInParent<MastInteraction>() ) {
			if (repairPattern != null && repairPattern.gameObject.activeInHierarchy) {//
				return;
			}

			timer = 0;
			active = true;
			GetComponent<Renderer>().enabled = true;

			int rng = Random.Range(0, repairPatterns.Length);
			repairPattern = repairPatterns[rng];

			foreach (var pat in repairPatterns) {
				pat.gameObject.SetActive(false);
			}

			activator = other.transform.root;

			//repairPattern.gameObject.SetActive( false );//
		}
	}

	private void OnTriggerExit( Collider other ) {
		if (other.transform.root != activator) {
			return;
		}

		active = false;
		repairPattern = null;
		activator = null;
	}

}
