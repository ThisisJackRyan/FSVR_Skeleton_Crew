using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieAfterCycle : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Invoke("Die", 2f);
	}
	
	// Update is called once per frame
	void Die () {
		Destroy( gameObject );
	}
}
