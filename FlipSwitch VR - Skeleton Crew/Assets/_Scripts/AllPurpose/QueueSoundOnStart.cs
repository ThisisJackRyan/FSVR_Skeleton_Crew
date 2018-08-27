using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueueSoundOnStart : MonoBehaviour {

	public AudioClip clip;
	public float delay = 0.5f;
	// Use this for initialization
	void Start () {
		GetComponent<AudioSource>().PlayDelayed( delay );
		Invoke( "Play", GetComponent<AudioSource>().clip.length + delay );
	}
	
	// Update is called once per frame
	void Play () {
		GetComponent<AudioSource>().PlayOneShot( clip );
	}
}
