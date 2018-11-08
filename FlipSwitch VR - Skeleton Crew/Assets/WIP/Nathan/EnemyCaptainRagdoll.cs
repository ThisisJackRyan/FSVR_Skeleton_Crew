using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyCaptainRagdoll : NetworkBehaviour {

	public AudioClip willHelmScream;
	public float timeToPlayScream = 2.5f;

	private AudioSource source;
	private float duration;
	private bool hasScreamed;

	// Use this for initialization
	void Start () {
		source = GetComponent<AudioSource>();
	}

	private void Update() {
		if (!isServer) {
			return;
		}

		if(duration >= timeToPlayScream && !hasScreamed) {
			hasScreamed = true;
			PlayScream();
		} else {
			duration += Time.deltaTime;
		}
	}

	private void PlayScream() {
		RpcPlayScream();
		source.clip = willHelmScream;
		source.Play();
	}

	[ClientRpc]
	private void RpcPlayScream() {
		if (isServer) {
			return;
		}

		source.clip = willHelmScream;
		source.Play();
	}
}
