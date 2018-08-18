using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class HatchActivator : NetworkBehaviour {

	float timer = 0;
	bool active = false;

	public static List<HatchActivator> hatches = new List<HatchActivator> ();
	public GameObject hatchSign;
	public bool isLeftHatch;

	public static HatchActivator instance;

	private void OnEnable() {
		print("on enable");
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
	
	//public static void TellRpcToDisableHatches(){
	//	instance.RpcDisableHatches(isLeftHatch);
	//}

	private void OnTriggerStay(Collider other) {
		if (!isServer)
			return;

		if (other.gameObject.GetComponentInParent<MastInteraction>() && active) {
			timer += Time.deltaTime;

			if (timer >= 1) {
				active = false;
				timer = 0;
				Ratman.RespawnRatmen(transform.position, isLeftHatch);
			}
		}
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
