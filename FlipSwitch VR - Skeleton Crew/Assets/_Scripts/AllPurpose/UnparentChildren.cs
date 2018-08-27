using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnparentChildren : MonoBehaviour {

	// Use this for initialization
	void OnEnable () {

            transform.DetachChildren();
        Destroy(gameObject);
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
