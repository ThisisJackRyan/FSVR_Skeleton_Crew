using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieAfterCycle : MonoBehaviour {

    public float particleLifetime;

	// Use this for initialization
	void Start () {
		//Debug.Break();
		Invoke("Die", 5f );
	}
	
	// Update is called once per frame
	void Die () {
		Destroy( gameObject );
	}
}
