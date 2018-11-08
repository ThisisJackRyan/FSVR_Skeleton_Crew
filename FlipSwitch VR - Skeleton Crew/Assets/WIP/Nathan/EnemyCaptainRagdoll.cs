using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyCaptainRagdoll : NetworkBehaviour {

	public AudioClip willHelmScream;
	public float timeToPlayScream = 0.5f;
	public GameObject deathExplosion;

	private AudioSource source;
	private float duration;
	private bool hasScreamed;
	private GameObject explosionInstance;
	public float forceDistance = 1.5f, forceRadius = 2f, explosionForce = 10, upForce = 1.5f;

	// Use this for initialization
	void Start () {
		source = GetComponent<AudioSource>();

		if (isServer) {
			explosionInstance = Instantiate(deathExplosion, transform.position, Quaternion.identity);
			NetworkServer.Spawn(explosionInstance);

			var cols = Physics.OverlapSphere(transform.position, 2);
			foreach(var c in cols) {
				if (c.GetComponent<Rigidbody>()) {
					c.GetComponent<Rigidbody>().AddExplosionForce(explosionForce, transform.position + ((transform.forward * -1) * forceDistance), forceRadius, upForce, ForceMode.Impulse);
				}
			}

			Invoke("DestroyExplosion", 3f);
		}
	}

	private void OnDrawGizmosSelected() {
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(transform.position + ((transform.forward * -1) * forceDistance), forceRadius);
	}



	private void DestroyExplosion() {
		if (!isServer) {
			return;
		}

		NetworkServer.Destroy(explosionInstance);
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
