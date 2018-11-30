using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableParticlesAfterDelay : MonoBehaviour {

	public GameObject toEnable;
	public float delay = 0.5f;

	// Use this for initialization
	void Start () {
		Invoke("EnableAfterDelay", delay);
	}
	
	// Update is called once per frame
	void EnableAfterDelay () {
		toEnable.SetActive(true);
	}
}
