using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCaptainDialogueOnStartOrWhenInRange : MonoBehaviour {

	public bool playOnStart = true, playWhenInRange = false;
	public float range = 25f;
	bool inRange = false;
	public AudioClip startClip, rangeClip;

	// Use this for initialization
	void Start () {
		if (!playOnStart) {
			return;
		}

		Captain.instance.PlayDialogue( startClip.name );
	}
	
	// Update is called once per frame
	void Update () {
		if (inRange) {
			return;
		}

		if (Mathf.Abs(Vector3.Distance(transform.position, Captain.instance.transform.position)) < range) {
			Captain.instance.PlayDialogue( rangeClip.name );
			inRange = true;
		}
	}
}
