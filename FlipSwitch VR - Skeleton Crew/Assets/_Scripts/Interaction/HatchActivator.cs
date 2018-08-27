using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class HatchActivator : NetworkBehaviour {

	float timer = 0;
	bool active = false;

	public static List<HatchActivator> hatches = new List<HatchActivator> ();
	public GameObject hatchSign;
	public Animator animator;
	public AudioSource audioSource;
	public AudioClip openClip;
	public bool isLeftHatch;

	public static HatchActivator instance;

	private void OnEnable() {
		//print("on enable");
		hatches.Add(this);
		GetComponent<Collider>().enabled = false;
		hatchSign.SetActive(false);

		if (!instance) {
			instance = this;
		}
	}

	private void OnDisable() {
		hatches.Remove(this);
	}

	public static void EnableHatch(bool isLeftHatch) {
		foreach ( var h in hatches ) {
			if ( h.isLeftHatch == isLeftHatch ) {
				h.GetComponent<Collider>().enabled = true;
				h.hatchSign.SetActive( true );
			}
		}
	}

	public static void DisableHatch(bool isLeftHatch) {
		foreach (var h in hatches) {
			if (h.isLeftHatch == isLeftHatch) {
				h.GetComponent<Collider>().enabled = false;
				h.hatchSign.SetActive( false );
			}
		   
		}
	}

	[ClientRpc]
	public void RpcDisableHatch(bool isLeftHatch) {
		if (isServer)
			return;
		
		DisableHatch(isLeftHatch);
	}


	[ClientRpc]
	public void RpcAnimateHatch(bool opening ) {
		if (isServer) {
			return;
		}

		animator.SetBool( "Opening", opening );
		audioSource.PlayOneShot( openClip );

	}

	private void OnTriggerStay(Collider other) {
		if (!isServer)
			return;

		if (other.gameObject.GetComponentInParent<MastInteraction>() && active) {
			timer += Time.deltaTime;

			if (timer >= 1) {
				active = false;
				timer = 0;
				StartCoroutine("AnimateAndRespawnRatmen");
			}
		}
	}

	IEnumerator AnimateAndRespawnRatmen() {
		animator.SetBool( "Opening", true );
		audioSource.PlayOneShot(openClip);
		RpcAnimateHatch( true );
		yield return new WaitForSeconds(1);
		bool hasRespawned = false;

		foreach ( var item in VariableHolder.instance.ratmenPositions ) {
			if ( item.Value != isLeftHatch ) {
				continue;
			}

			if ( item.Key.GetComponent<Ratman>().GetHealth() <= 0 ) {
				item.Key.GetComponent<Ratman>().Respawn(  transform.position );
				hasRespawned = true;
				yield return new WaitForSeconds( 1 );
			}
		}

		if ( hasRespawned ) {
			DisableHatch( isLeftHatch );
			instance.RpcDisableHatch( isLeftHatch );
		}

		animator.SetBool( "Opening", false );
		audioSource.PlayOneShot( openClip );

		RpcAnimateHatch( false );

		yield return new WaitForSeconds( 1 );
	}

	void RespawnRatmen(Vector3 spawnPos, bool isLeftHatch) {

	}

	private void OnTriggerEnter(Collider other) {
		if (!isServer)
			return;

		if (other.gameObject.GetComponentInParent<MastInteraction>() && !active) {
			timer = 0;
			active = true;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (!isServer)
			return;

		active = false;
	}
}
