using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class HatchActivator : NetworkBehaviour {

    float timer = 0;
    bool active = false;

    public static List<HatchActivator> hatches = new List<HatchActivator> ();
    public GameObject hatchSign;

    public static HatchActivator instance;

    private void OnEnable() {
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

    public static void EnableHatches() {

        foreach (var h in hatches) {
            h.GetComponent<Collider>().enabled = true;
            h.hatchSign.SetActive(true);
        }
    }

    public static void DisableHatches() {
        foreach (var h in hatches) {
            h.GetComponent<Collider>().enabled = false;
            h.hatchSign.SetActive(false);

        }
    }

    [ClientRpc]
    public void RpcDisableHatches() {
        if (isServer)
            return;
        
        DisableHatches();
    }
	
	public static void TellRpcToDisableHatches(){
		instance.RpcDisableHatches();
	}

    private void OnTriggerStay(Collider other) {
        if (!isServer)
            return;

        if (other.gameObject.GetComponentInParent<MastInteraction>() && active) {
            timer += Time.deltaTime;

            if (timer >= 1) {
                active = false;
                timer = 0;
                Ratman.RespawnRatmen(transform.position);
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
