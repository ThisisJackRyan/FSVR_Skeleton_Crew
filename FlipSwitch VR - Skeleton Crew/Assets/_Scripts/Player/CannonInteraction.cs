using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CannonInteraction : NetworkBehaviour {

	public void Fire(GameObject cannon) {
		if (!isServer)
			return;

		FireCannon(cannon);
	}

	private void FireCannon(GameObject cannon) {
		cannon.GetComponent<Cannon>().CreateCannonBall();
		Captain.playersFiredCannons[this] = true;
		Captain.instance.CheckPlayersCannonFiring();

		RpcFireCannon(cannon);
	}

	[ClientRpc]
	private void RpcFireCannon(GameObject cannon) {
		if (isServer)
			return;
		cannon.GetComponent<Cannon>().CreateCannonBall();
	}

	private void Start() {
		if (isServer) {
			print(name + " enabled server check");
			Captain.playersFiredCannons.Add(this, false);
		}

		mastInteraction = GetComponent<MastInteraction>();
	}

	#region Cannon aiming

	public float maxReachToCannonWheel = 2f;

	MastInteraction mastInteraction;
	Cannon cannonCurrentlyAiming;
	int indexOfClosest = -1;

	void Update() {
		if ( !isLocalPlayer ) {
			return;
		}

		//closest hasnt been found, grabbing is allowed
		if ( indexOfClosest == -1 && mastInteraction.emptyLeftHand && Controller.LeftController.GetPressDown(Controller.Grip)) {
			//print("inside button down left");
			Collider[] hits = Physics.OverlapSphere( mastInteraction.leftHand.position, mastInteraction.radius );
			for (int i = 0; i < hits.Length; i++) {
				if (hits[i].transform.tag == "CannonAimingWheel") {
					cannonCurrentlyAiming = hits[i].GetComponentInParent<Cannon>();
					//get closest node
					Transform closest = null;
					for (int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++) {
						Transform node = cannonCurrentlyAiming.aimingNodes[index];
						node.GetComponent<Renderer>().enabled = true;
						node.GetComponent<CannonAimNode>().player = this;

						if ( !closest) {
							closest = node;
							indexOfClosest = index;
						} else {
							if (Mathf.Abs(Vector3.Distance(mastInteraction.leftHand.position, hits[i].transform.position)) <
							    Mathf.Abs(Vector3.Distance(mastInteraction.leftHand.position, closest.transform.position))) {
								closest = node;
								indexOfClosest = index;
							}
						}
					}

					//got closest node
					cannonCurrentlyAiming.indexOfFirstGrabbed = indexOfClosest;
					mastInteraction.leftHandInteracting = true;
				}
			}
		}

		if ( indexOfClosest == -1 && mastInteraction.emptyRightHand && Controller.RightController.GetPressDown(Controller.Grip)) {
			//print( "inside button down right" );

			Collider[] hits = Physics.OverlapSphere( mastInteraction.rightHand.position, mastInteraction.radius );
			for ( int i = 0; i < hits.Length; i++) {
				if (hits[i].transform.tag == "CannonAimingWheel" ) {
					cannonCurrentlyAiming = hits[i].GetComponentInParent<Cannon>();
					Transform closest = null;
					for ( int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++ ) {
						Transform node = cannonCurrentlyAiming.aimingNodes[index];
						node.GetComponent<Renderer>().enabled = true;
						node.GetComponent<CannonAimNode>().player = this;

						if ( !closest ) {
							closest = node;
							indexOfClosest = index;
						} else {
							if ( Mathf.Abs( Vector3.Distance( mastInteraction.rightHand.position, hits[i].transform.position ) ) <
								Mathf.Abs( Vector3.Distance( mastInteraction.rightHand.position, closest.transform.position ) ) ) {
								closest = node;
								indexOfClosest = index;
							}
						}
					}

					cannonCurrentlyAiming.indexOfFirstGrabbed = indexOfClosest;

					mastInteraction.rightHandInteracting = true;
					//Debug.Break();
				}
			}
		}

		

		//player has grabbed wheel
		if ( indexOfClosest >= 0 && mastInteraction.leftHandInteracting && Controller.LeftController.GetPress( Controller.Grip ) ) {
			//print( "inside hold down left" );
			//interacting with wheel
			//needs to have constant hit detction, if hit index lower than closest then lower aim, else raaise it
			//var lookPos = mastInteraction.leftHand.position - cannonCurrentlyAiming.handMarker.position;
			//lookPos.x = 0;
			//var rotation = Quaternion.LookRotation( lookPos );
			//cannonCurrentlyAiming.handMarker.rotation = Quaternion.Slerp( transform.rotation, rotation, Time.deltaTime );

			//cannonCurrentlyAiming.handMarker.LookAt( mastInteraction.leftHand );
			//var rot = cannonCurrentlyAiming.handMarker.rotation;
			if (Vector3.Distance( mastInteraction.leftHand.position, cannonCurrentlyAiming.aimingNodes[indexOfClosest].position )> maxReachToCannonWheel) {
				StopInteracting();
			}	
		}

		if ( indexOfClosest >= 0 && mastInteraction.rightHandInteracting && Controller.RightController.GetPress( Controller.Grip ) ) {
			//interacting with wheel
			//print( "inside hold down right" );
			if ( Vector3.Distance( mastInteraction.rightHand.position, cannonCurrentlyAiming.aimingNodes[indexOfClosest].position ) > maxReachToCannonWheel ) {
				StopInteracting();
			}
		}


		//player let go
		if (mastInteraction.leftHandInteracting && Controller.LeftController.GetPressUp( Controller.Grip ) ) {
			//print( "inside up left" );
			mastInteraction.leftHandInteracting = false;
			StopInteracting();
		}

		if (mastInteraction.rightHandInteracting && Controller.RightController.GetPressUp( Controller.Grip ) ) {
			//print( "inside up right" );
			mastInteraction.rightHandInteracting = false;
			StopInteracting();
		}
	}


	public void StopInteracting() {
		for ( int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++ ) {
			Transform node = cannonCurrentlyAiming.aimingNodes[index];
			node.GetComponent<Renderer>().enabled = false;
			node.GetComponent<CannonAimNode>().player = null;
		}

		cannonCurrentlyAiming.indexOfFirstGrabbed = -1;
		cannonCurrentlyAiming = null;
		indexOfClosest = -1;
	}

	#endregion
}