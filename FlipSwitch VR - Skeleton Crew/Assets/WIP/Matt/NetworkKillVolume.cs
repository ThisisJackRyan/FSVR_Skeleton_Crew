using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class NetworkKillVolume : NetworkBehaviour {

    private void OnCollisionEnter(Collision collision) {
        OnTriggerEnter(collision.collider);
    }

    private void OnTriggerEnter(Collider other) {
        if (!isServer) {
            if (!other.GetComponent<NetworkIdentity>()) {
                Destroy(other.gameObject);
            }
            return;
        }

        //print("killing " + other.name + " with networkkillvolume");
        if (other.GetComponent<NetworkIdentity>()) {
            NetworkServer.Destroy(other.gameObject);
        } else {
            Destroy(other.gameObject);
        }
    }
}
