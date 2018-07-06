using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: BoundaryTrigger
/// </summary>
public class BoundaryTrigger : MonoBehaviour {
	#region Fields

	public GameObject grid;

	#endregion

	void Start() {
		grid.SetActive(false);
	}

	private void OnTriggerEnter(Collider other) {
		if (other.transform.root.tag == "Player") {
			//HapticHelper.instance.ProceduralTone(true, 255, 20);
			//HapticHelper.instance.ProceduralTone(false, 255, 20);

			grid.SetActive(true);
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.transform.root.tag == "Player") {
			grid.SetActive(false);
		}
	}

	private void OnTriggerStay(Collider other) {
		if (other.transform.root.tag == "Player") {
			//HapticHelper.instance.ProceduralTone( true, 255, 20 );
			////HapticHelper.instance.ProceduralTone(false, 255, 20);
			//HapticHelper.instance.GenerateSinPulse(true, 255, 5);
			//HapticHelper.instance.GenerateSinPulse( false, 255, 5 );

		}
	}

}