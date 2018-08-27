using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmissionEnabler : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	public void TurnOnEmission () {
		GetComponent<Renderer>().materials[0].SetColor("_EmissionColor", Color.white);
    }
    public void TurnOffEmission()
    {
        GetComponent<Renderer>().materials[0].SetColor("_EmissionColor", Color.black);
    }
}
