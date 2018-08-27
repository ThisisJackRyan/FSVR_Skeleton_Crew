using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayRandomAnimation : MonoBehaviour {

	public AnimationClip[] clips;

	// Use this for initialization
	void Start () {
		foreach (var animationClip in clips) {
			GetComponent<Animation>().AddClip(animationClip, animationClip.name);
		}
		StartAnims();
	}

	void StartAnims() {
		StopAllCoroutines();

		StartCoroutine( "PlayAnim" );
	}

	// Update is called once per frame
	IEnumerator PlayAnim () {
		//print("called");
		int i = Random.Range(0, clips.Length);
		GetComponent<Animation>().Play(clips[i].name);
		yield return new WaitForSeconds(clips[i].length);
		StartAnims();
	}
}
