using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MastInteraction : NetworkBehaviour {

	public bool emptyLeftHand;
	public bool emptyRightHand;
	public float radius = 0.1f;
    public float maxReachToMastWheel = 0.5f;

    public GameObject grabPoint, handPoint;
	public Transform rightHand, leftHand;
	public bool leftHandInteracting;
	public bool rightHandInteracting;
	public MastSwitch mast;
    public Transform[] aimingNodes;
    public int indexOfClosest = -1;



    //internal GameObject selected;

    private void Start() {
		emptyLeftHand = true;
		emptyRightHand = true;
		mast = FindObjectOfType<MastSwitch>();		
	}

    void Update() {
        if (!isLocalPlayer) {
            return;
        }

        //closest hasnt been found, grabbing is allowed  //are we changing the -1 sentinel?
        if (!leftHandInteracting && emptyLeftHand && Controller.LeftController.GetPressDown(Controller.Grip)) {
            CmdHandleAiming(true);
            //print("inside button down left");
        }

        if (!rightHandInteracting && emptyRightHand && Controller.RightController.GetPressDown(Controller.Grip)) {
            CmdHandleAiming(false);
        }

        //player has grabbed wheel
        if (leftHandInteracting && Controller.LeftController.GetPress(Controller.Grip)) {
            if (Vector3.Distance(leftHand.position, aimingNodes[indexOfClosest].position) > maxReachToMastWheel) {
                leftHandInteracting = false;
                CmdStopInteracting(true, false);
            }
        }

        if (rightHandInteracting && Controller.RightController.GetPress(Controller.Grip)) {
            if (Vector3.Distance(rightHand.position, aimingNodes[indexOfClosest].position) > maxReachToMastWheel) {
                rightHandInteracting = false;
                CmdStopInteracting(false, false);
            }
        }

        //player let go
        if (leftHandInteracting && Controller.LeftController.GetPressUp(Controller.Grip)) {
            //print( "inside up left" );
            leftHandInteracting = false;
            CmdStopInteracting(true, true);
        }

        if (rightHandInteracting && Controller.RightController.GetPressUp(Controller.Grip)) {
            //print( "inside up right" );
            rightHandInteracting = false;
            CmdStopInteracting(false, true);
        }
    }

    //todo Captain.instance.MastHasBeenPulled(); still needs to be called when mast is pulled/changed

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

        foreach (var node in cannon.GetComponentInChildren<AngleSetterTrigger>().nodes) {
            node.SetActive(false);
        }
        foreach ( CannonAimNode node in cannon.GetComponentsInChildren<CannonAimNode>() ) {
			node.particles.SetActive( true);
		}

		cannonCurrentlyAiming = cannon.GetComponent<Cannon>();
		indexOfClosest = iOfClosest;

		if (isLeft) {
			leftHandInteracting = true;
		} else {
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

        cannon.GetComponentInChildren<AngleSetterTrigger>().TurnOffNodes();

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
		cannonCurrentlyAiming = null;
		indexOfClosest = -1;
	}



    [ClientRpc]
    public void RpcTurnOffHintNodes(GameObject cannon) {
        cannon.GetComponentInChildren<AngleSetterTrigger>().TurnOffNodes();
    }

    [ClientRpc]
    public void RpcTurnONHintNodes(GameObject cannon) {
        cannon.GetComponentInChildren<AngleSetterTrigger>().TurnONNodes();
    }

}
