using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintSiblingIndex : MonoBehaviour {

	// Use this for initialization
	void Start () {
		print(transform.GetSiblingIndex());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
