using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MastSwitch : NetworkBehaviour {

	public Animator sailAnimator;
	public float speedIncrement = 0.3f;
    public int indexOfFirstGrabbed = -1; //only being set on local player
    
	public PathFollower pathFollower;
	public AudioClip raise, lower;
    public AudioClip aimClip;
    public GameObject[] aimingNodes;

    AudioSource source;

	private void Start() {
		source = GetComponent<AudioSource>();
	}

    public void AdjustSails(int indexOfNode) {
        if (!isServer) {
            return;
        }

        if (indexOfFirstGrabbed >= 0) {
            int raiseSign = (indexOfNode > indexOfFirstGrabbed) ? -1 : 1; //if index is greater (closer to back of cannon) then you are raising the cannon

            bool playSound = pathFollower.ChangeSpeed(speedIncrement * raiseSign);

            if (playSound) {
                PlayAimSound();
            }

            indexOfFirstGrabbed = indexOfNode;

            RpcAdjustSails(pathFollower.speed );

            //todo add animator code here

        }
    }

    [ClientRpc]
    public void RpcAdjustSails(float newSpeed) {
        if (isServer) {
            return;
        }
        //do local animation set here, may not need if using network animator
    }


    [ClientRpc]
    public void RpcPlayAimSound() {
        if (isServer) {
            return;
        }
        GetComponent<AudioSource>().PlayOneShot(aimClip);
    }

    public void PlayAimSound() {
        if (!isServer) {
            return;
        }
        GetComponent<AudioSource>().PlayOneShot(aimClip);
        RpcPlayAimSound();
    }

}