using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableRenderers : MonoBehaviour {

	// Use this for initialization
	void LateUpdate () {
	
		foreach(var r in GetComponentsInChildren<Renderer>() ) {
			//print(r.name);
			r.enabled = true;
		}
	}

	//private void LateUpdate() {

	//}



}
