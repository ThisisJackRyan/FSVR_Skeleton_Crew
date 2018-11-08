using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyCaptainRagdoll : NetworkBehaviour {

	public AudioClip willHelmScream;

	private AudioSource source;
	private Rigidbody rb;
	
	// Use this for initialization
	void Start () {
		source = GetComponent<AudioSource>();	
	}

	private void Update() {
		if (!isServer) {
			return;
		}

		if(rb.velocity.magnitude > 3) {
			PlayScream();
		} else {
			print("velocity is " + rb.velocity.magnitude);
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
