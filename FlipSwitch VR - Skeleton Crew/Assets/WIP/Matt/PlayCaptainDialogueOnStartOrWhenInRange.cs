using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayCaptainDialogueOnStartOrWhenInRange : NetworkBehaviour {

	public bool playOnStart = true, playWhenInRange = false;
	public float range = 25f;
	bool inRange = false;
	public AudioClip startClip, rangeClip;

	// Use this for initialization
	void Start () {
        if (!isServer) {
            return;
        }

		if (!playOnStart) {
			return;
		}

		Captain.instance.PlayDialogue( startClip.name );
	}
	
	// Update is called once per frame
	void Update () {
        if (!isServer || inRange || !playWhenInRange) {
            return;
        }

		if (Mathf.Abs(Vector3.Distance(transform.position, Captain.instance.transform.position)) < range) {
			Captain.instance.PlayDialogue( rangeClip.name );
			inRange = true;
		}
	}
}
