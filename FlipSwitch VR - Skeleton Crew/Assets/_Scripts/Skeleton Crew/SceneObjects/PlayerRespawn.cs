﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerRespawn : NetworkBehaviour {

    float timer = 0;
    bool active = false;
	bool isRespawning = false;
    GameObject playerBeingRevived = null;
	public GameObject animObject;
	private GameObject animInstance;


	private void OnTriggerStay(Collider other) {
        if (!isServer)
            return;

        if (other.gameObject.tag == "PlayerCollider" && active) {
            timer += Time.deltaTime;

            if (timer >= 1) {
                active = false;
                timer = 0;
				StartRespawnAnimation();
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isServer)
            return;

        if (other.gameObject.tag == "PlayerCollider" && !active) {
	        if (other.GetComponentInParent<Player>().GetHealth() <= 0) {
		        timer = 0;
		        active = true;
		        playerBeingRevived = other.transform.root.gameObject;
	        }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!isServer)
            return;

		if ( other.gameObject == playerBeingRevived ) {
			if ( isRespawning ) {
				StopRespawnAnimation();
			}
            active = false;
            playerBeingRevived = null;
        }
    }


	private void StopRespawnAnimation() {
		isRespawning = false;
		CancelInvoke();
	}



	private void StartRespawnAnimation() {
		isRespawning = true;
		//animInstance.GetComponent<ObjectPositionLock>().posPoint =
		//	playerBeingRevived.GetComponentInChildren<HipMarker>().gameObject;
		animInstance = Instantiate( animObject, playerBeingRevived.GetComponentInChildren<HipMarker>().gameObject.transform.position, Quaternion.identity );
		RpcStartRespawnAnimation( playerBeingRevived);
		Invoke( "RespawnPlayer", animObject.GetComponentInChildren<Animation>().clip.length );
	}

	[ClientRpc]
	private void RpcStartRespawnAnimation(GameObject player) {
		if (isServer) {
			return;
		}

		animInstance = Instantiate( animObject, transform.position, Quaternion.identity );
		animInstance.GetComponent<ObjectPositionLock>().posPoint =
			player.GetComponentInChildren<HipMarker>().gameObject;
	}

	void RespawnPlayer() {
		isRespawning = false;
		playerBeingRevived.GetComponent<Player>().RevivePlayer();
	}
}
