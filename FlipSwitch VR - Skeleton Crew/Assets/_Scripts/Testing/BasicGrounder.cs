using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicGrounder : MonoBehaviour {

	// Use this for initialization
	void Start () {

		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down,out hit )) {
			transform.parent.Translate(hit.point - transform.position);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
