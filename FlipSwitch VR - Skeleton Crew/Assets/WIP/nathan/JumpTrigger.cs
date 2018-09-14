using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Opsive.ThirdPersonController.Abilities;

public class JumpTrigger : NetworkBehaviour {

    private void Start() {
        if (isClient && !isServer) {
            GetComponent<BoxCollider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if(other.GetComponent<Jump>()) {
            other.GetComponent<Jump>().StartAbility();
        }
    }
}
