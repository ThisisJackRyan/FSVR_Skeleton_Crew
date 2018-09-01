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
    public AudioClip aimClip, maxClip;
    public GameObject[] aimingNodes;

    AudioSource source;

	private void Start() {
		source = GetComponent<AudioSource>();
	}

    public void AdjustSails(int indexOfNode) {
        if (!isServer) {
            return;
        }
        //print("index first grabbed " + indexOfFirstGrabbed);
        //print("index of node " + indexOfNode);

        if (indexOfFirstGrabbed >= 0) {
            int raiseSign = (indexOfNode > indexOfFirstGrabbed) ? -1 : 1; //if index is greater (closer to back of cannon) then you are raising the cannon

            bool playSound = pathFollower.ChangeSpeed(speedIncrement * raiseSign);

           
            PlayAimSound(playSound);
            

            indexOfFirstGrabbed = indexOfNode;

            RpcAdjustSails(pathFollower.speed );

            sailAnimator.SetFloat("Speed",pathFollower.speed);

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
    public void RpcPlayAimSound(bool notMax) {
        if (isServer) {
            return;
        }

        if (notMax) {
            GetComponent<AudioSource>().PlayOneShot(aimClip);
        } else {
            GetComponent<AudioSource>().PlayOneShot(maxClip);
        }
    }

    public void PlayAimSound(bool notMax) {
        if (!isServer) {
            return;
        }

        if (notMax) {
            GetComponent<AudioSource>().PlayOneShot(aimClip);
        } else {
            GetComponent<AudioSource>().PlayOneShot(maxClip);
        }
        
        RpcPlayAimSound(notMax);
    }

}