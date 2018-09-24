using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnparentChildren : MonoBehaviour {

	// Use this for initialization
	void Awake () {

            transform.DetachChildren();
        Destroy(gameObject);
        
	}
	
}
