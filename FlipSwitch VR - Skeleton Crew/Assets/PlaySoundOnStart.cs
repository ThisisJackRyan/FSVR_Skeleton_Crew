using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySoundOnStart : MonoBehaviour {

	public AudioClip[] clips;

	// Use this for initialization
	void Start () {
		if ( clips.Length > 0 ) {
			int rng = Random.Range( 0, clips.Length );
			GetComponent<AudioSource>().PlayOneShot(clips[rng]);
		}
	}

}
