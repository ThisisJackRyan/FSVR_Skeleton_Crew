using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairTrigger : MonoBehaviour {

	public DamagedObject dmgObj;
	public GameObject particles;
	[HideInInspector]
	public RepairPattern repairPattern;
	Transform activator;
	public GameObject burst;

	float timer = 0;
	bool active = false;

	private void OnTriggerStay(Collider other) {
        if (other.transform.root != activator || !active) {
            return;
        }

        timer += Time.deltaTime;

		if (timer >= 1) {
			repairPattern.gameObject.SetActive(true); //
			repairPattern.Init(); //

			particles.SetActive(false);
			//tracePrompt.SetActive(true);
			dmgObj.SpawnBurst(burst, transform.position);
			//Controller.PlayHaptics(other.gameObject.GetComponent<GrabWeaponHand>().isLeftHand, HapticController.BurstHaptics);

			transform.position = new Vector3(transform.position.x,
				                                other.transform.root.GetComponentInChildren<HipMarker>().transform.position.y,
				                                transform.position.z);

			dmgObj.EnablePatternOnClients(); // <-- Enables pattern & disables particles on clients

			active = false;
			activator = null;
		}
	}

	private void OnTriggerEnter(Collider other) {
        //print("Triggered by " + other.name);
        if (!GetComponentInParent<DamagedObject>().isServer) {
            //print("returning as not serer");
            return;
        }
		if (other.gameObject.GetComponentInParent<MastInteraction>()) { //player check
			if (repairPattern != null && repairPattern.gameObject.activeInHierarchy) { //pattern is active
				return;
			}
			
			timer = 0;
			active = true;
			//particles.SetActive(true);
			//tracePrompt.SetActive(false);

			repairPattern = dmgObj.SelectPattern();

			activator = other.transform.root;

			//repairPattern.gameObject.SetActive( false );//
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.transform.root != activator) {
			return;
		}

		active = false;

		//repairPattern = null;
		//activator = null;
	}

	private void OnDisable() {
		//print("repoaIR NODE DISABLED");
	}

    private void OnEnable() {
        //print("repair sphere has been enabled. Should be setting the particles to active. Disabling all other children. Should effectively initialize the repairing.");
        for(int i=0; i<transform.childCount; i++) {
            if (i == 0) {
                transform.GetChild(i).gameObject.SetActive(true);
                continue;
            }
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}