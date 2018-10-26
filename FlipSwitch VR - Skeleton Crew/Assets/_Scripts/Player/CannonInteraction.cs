using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CannonInteraction : NetworkBehaviour {

	public void Fire( GameObject cannon ) {
		if ( !isServer )
			return;

		FireCannon( cannon );
	}

	private void FireCannon( GameObject cannon ) {
		cannon.GetComponent<Cannon>().CreateCannonBall();
		if ( !SceneManager.GetActiveScene().name.Contains( "Boss" ) ) {
			Captain.playersFiredCannons[this] = true;
			Captain.instance.CheckPlayersCannonFiring();
		}

		RpcFireCannon( cannon );
	}

	[ClientRpc]
	private void RpcFireCannon( GameObject cannon ) {
		if ( isServer )
			return;
		cannon.GetComponent<Cannon>().CreateCannonBall();
	}

	private void Start() {
		if ( isServer ) {
			////print( name + " enabled server check" );
			Captain.playersFiredCannons.Add( this, false );
		}

		mastInteraction = GetComponent<MastInteraction>();
	}

	#region Cannon aiming

	public float maxReachToCannonWheel = 0.5f;

	MastInteraction mastInteraction;
	Cannon cannonCurrentlyAiming;
	public int indexOfClosest = -1;
	bool leftHandInteracting, rightHandInteracting;

	void Update() {
		if ( !isLocalPlayer ) {
			return;
		}

		//closest hasnt been found, grabbing is allowed  //are we changing the -1 sentinel?
		if ( !leftHandInteracting && mastInteraction.emptyLeftHand && Controller.LeftController.GetPressDown( Controller.Grip ) ) {
			CmdHandleAiming( true );
			////print("inside button down left");
		}

		if (!rightHandInteracting && mastInteraction.emptyRightHand && Controller.RightController.GetPressDown( Controller.Grip ) ) {
			CmdHandleAiming( false );
		}

		//player has grabbed wheel
		if (leftHandInteracting && Controller.LeftController.GetPress( Controller.Grip ) ) {
			if ( Vector3.Distance( mastInteraction.leftHand.position, cannonCurrentlyAiming.aimingNodes[indexOfClosest].position ) > maxReachToCannonWheel ) {
				leftHandInteracting = false;
				CmdStopInteracting(true, false);
			}
		}

		if (rightHandInteracting && Controller.RightController.GetPress( Controller.Grip ) ) {
			if ( Vector3.Distance( mastInteraction.rightHand.position, cannonCurrentlyAiming.aimingNodes[indexOfClosest].position ) > maxReachToCannonWheel ) {
				rightHandInteracting = false;
				CmdStopInteracting(false, false);
			}
		}

		//player let go
		if ( leftHandInteracting && Controller.LeftController.GetPressUp( Controller.Grip ) ) {
			////print( "inside up left" );
			leftHandInteracting = false;
			CmdStopInteracting(true, true);
		}

		if ( rightHandInteracting && Controller.RightController.GetPressUp( Controller.Grip ) ) {
			////print( "inside up right" );
			rightHandInteracting = false;
			CmdStopInteracting(false, true);
		}
	}

	[Command]
	private void CmdHandleAiming( bool isLeft ) {
		if ( isLeft ) {
			Collider[] hits = Physics.OverlapSphere( mastInteraction.leftHand.position, mastInteraction.radius );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "CannonAimingWheel" ) {
					cannonCurrentlyAiming = hits[i].GetComponentInParent<Cannon>();
					//get closest node
					Transform closest = null;
					for ( int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++ ) {
						Transform node = cannonCurrentlyAiming.aimingNodes[index];
						node.GetComponent<CannonAimNode>().player = this;
						if ( !closest ) {
							closest = node;
							indexOfClosest = index;
						} else {
							if ( Mathf.Abs( Vector3.Distance( mastInteraction.leftHand.position, hits[i].transform.position ) ) <
								Mathf.Abs( Vector3.Distance( mastInteraction.leftHand.position, closest.transform.position ) ) ) {
								closest = node;
								indexOfClosest = index;
							}
						}
					}

					RpcStartInteractingOnClient( cannonCurrentlyAiming.gameObject, gameObject, isLeft, indexOfClosest );
					//got closest node
					cannonCurrentlyAiming.indexOfFirstGrabbed = indexOfClosest;
					leftHandInteracting = true;
				}
			}
		} else {
			Collider[] hits = Physics.OverlapSphere( mastInteraction.rightHand.position, mastInteraction.radius );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "CannonAimingWheel" ) {
					cannonCurrentlyAiming = hits[i].GetComponentInParent<Cannon>();
					Transform closest = null;
					for ( int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++ ) {
						Transform node = cannonCurrentlyAiming.aimingNodes[index];
						//node.GetComponent<Renderer>().enabled = true;
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

					RpcStartInteractingOnClient( cannonCurrentlyAiming.gameObject, gameObject, isLeft, indexOfClosest );
					cannonCurrentlyAiming.indexOfFirstGrabbed = indexOfClosest;

					rightHandInteracting = true;
				}
			}
		}
	}

	[ClientRpc]
	private void RpcStartInteractingOnClient( GameObject cannon,GameObject player, bool isLeft, int iOfClosest ) {
		if (player != gameObject && !isServer) {
			return;
		}

        foreach (var node in cannon.GetComponentInChildren<CannonAngleSetterTrigger>().nodes) {
            node.SetActive(false);
        }
        foreach ( CannonAimNode node in cannon.GetComponentsInChildren<CannonAimNode>() ) {
			node.particles.SetActive( true);
		}

		cannonCurrentlyAiming = cannon.GetComponent<Cannon>();
        ////print(cannonCurrentlyAiming);

        indexOfClosest = iOfClosest;

		if (isLeft) {
            cannonCurrentlyAiming.GetComponentInChildren<RotationTester>().toFollow = GetComponent<GrabWeapon>().leftHand;
			leftHandInteracting = true;
		} else {
            cannonCurrentlyAiming.GetComponentInChildren<RotationTester>().toFollow = GetComponent<GrabWeapon>().rightHand;

            rightHandInteracting = true;
		}
	}

	[ClientRpc]
	private void RpcStopInteractingOnClient( GameObject cannon, GameObject player, bool isLeft, bool showMarkerNodes ) {
		if ( player != gameObject && !isServer ) {
			return;
		}

		foreach ( CannonAimNode node in cannon.GetComponentsInChildren<CannonAimNode>() ) {
			node.particles.SetActive( false );
		}

        cannon.GetComponentInChildren<CannonAngleSetterTrigger>().TurnOffNodes();

        if (!isServer) {

        cannonCurrentlyAiming.GetComponentInChildren<RotationTester>().toFollow = null;
        }


        cannonCurrentlyAiming = null;
		indexOfClosest = -1;
		if ( isLeft ) {
			leftHandInteracting = false;
		} else {
			rightHandInteracting = false;
		}
	}

	[Command]
	public void CmdStopInteracting(bool isLeft, bool showMarkerNodes) {
		for ( int index = 0; index < cannonCurrentlyAiming.aimingNodes.Length; index++ ) {
			Transform node = cannonCurrentlyAiming.aimingNodes[index];
			//node.GetComponent<Renderer>().enabled = false;
			node.GetComponent<CannonAimNode>().player = null;
		}

		RpcStopInteractingOnClient( cannonCurrentlyAiming.gameObject, gameObject, isLeft, showMarkerNodes );
		cannonCurrentlyAiming.indexOfFirstGrabbed = -1;

        cannonCurrentlyAiming
            .GetComponentInChildren<RotationTester>()
            .toFollow = null;

		cannonCurrentlyAiming = null;
		indexOfClosest = -1;

	}

    [ClientRpc]
    public void RpcTurnOffHintNodes(GameObject cannon) {
        cannon.GetComponentInChildren<CannonAngleSetterTrigger>().TurnOffNodes();
    }

    [ClientRpc]
    public void RpcTurnONHintNodes(GameObject cannon) {
        cannon.GetComponentInChildren<CannonAngleSetterTrigger>().TurnONNodes();
    }
    #endregion
}