using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerRespawn : NetworkBehaviour {

    float timer = 0;
    bool active = false;

    private void OnTriggerStay(Collider other) {
        if (!isServer)
            return;

        if (other.gameObject.tag == "PlayerCollider" && active) {
            timer += Time.deltaTime;

            if (timer >= 5) {
                active = false;
                timer = 0;
                other.gameObject.GetComponentInParent<ScriptSyncPlayer>().RevivePlayer();
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isServer)
            return;

        if (other.gameObject.tag == "PlayerCollider" && !active) {
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
