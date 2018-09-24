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
    public int indexOfClosest = -1;



    //internal GameObject selected;

    private void Start() {
		emptyLeftHand = true;
		emptyRightHand = true;
		//mast = FindObjectOfType<MastSwitch>();		
	}

    void Update() {
        if (!isLocalPlayer) {
            return;
        }

        //closest hasnt been found, grabbing is allowed  //are we changing the -1 sentinel?
        if (!leftHandInteracting && emptyLeftHand && Controller.LeftController.GetPressDown(Controller.Grip)) {
            CmdHandleAiming(true);
            ////print("inside button down left");
        }

        if (!rightHandInteracting && emptyRightHand && Controller.RightController.GetPressDown(Controller.Grip)) {
            CmdHandleAiming(false);
        }

        //player has grabbed wheel
        if (leftHandInteracting && Controller.LeftController.GetPress(Controller.Grip)) {
            //print("Index closest " + indexOfClosest);
            if (Vector3.Distance(leftHand.position, mast.aimingNodes[indexOfClosest].transform.position) > maxReachToMastWheel) {
                leftHandInteracting = false;
                CmdStopInteracting(true, false);
            }
        }

        if (rightHandInteracting && Controller.RightController.GetPress(Controller.Grip)) {
            if (Vector3.Distance(rightHand.position, mast.aimingNodes[indexOfClosest].transform.position) > maxReachToMastWheel) {
                rightHandInteracting = false;
                CmdStopInteracting(false, false);
            }
        }

        //player let go
        if (leftHandInteracting && Controller.LeftController.GetPressUp(Controller.Grip)) {
            ////print( "inside up left" );
            leftHandInteracting = false;
            CmdStopInteracting(true, true);
        }

        if (rightHandInteracting && Controller.RightController.GetPressUp(Controller.Grip)) {
            ////print( "inside up right" );
            rightHandInteracting = false;
            CmdStopInteracting(false, true);
        }
    }

    //todo Captain.instance.MastHasBeenPulled(); still needs to be called when mast is pulled/changed

        [Command]
	private void CmdHandleAiming( bool isLeft ) {
		if ( isLeft ) {
			Collider[] hits = Physics.OverlapSphere( leftHand.position, radius );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "MastAdjustmentWheel") {
					mast = hits[i].GetComponentInParent<MastSwitch>();

					//get closest node
					Transform closest = null;
					for ( int index = 0; index < mast.aimingNodes.Length; index++ ) {
						Transform node = mast.aimingNodes[index].transform;
						node.GetComponent<MastAimNode>().player = this;

						if ( !closest ) {
							closest = node;
							indexOfClosest = index;
						} else {
							if ( Mathf.Abs( Vector3.Distance( leftHand.position, hits[i].transform.position ) ) <
								Mathf.Abs( Vector3.Distance( leftHand.position, closest.transform.position ) ) ) {
								closest = node;
								indexOfClosest = index;
							}
						}
					}

					RpcStartInteractingOnClient( mast.gameObject, gameObject, isLeft, indexOfClosest );
					//got closest node
					mast.indexOfFirstGrabbed = indexOfClosest;
					leftHandInteracting = true;
				}
			}
		} else {
			Collider[] hits = Physics.OverlapSphere( rightHand.position, radius );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "MastAdjustmentWheel" ) {
					mast = hits[i].GetComponentInParent<MastSwitch>();

					Transform closest = null;
					for ( int index = 0; index < mast.aimingNodes.Length; index++ ) {
						Transform node = mast.aimingNodes[index].transform;
						//node.GetComponent<Renderer>().enabled = true;
						node.GetComponent<MastAimNode>().player = this;
						if ( !closest ) {
							closest = node;
							indexOfClosest = index;
						} else {
							if ( Mathf.Abs( Vector3.Distance( rightHand.position, hits[i].transform.position ) ) <
								Mathf.Abs( Vector3.Distance( rightHand.position, closest.transform.position ) ) ) {
								closest = node;
								indexOfClosest = index;
							}
						}
					}

					RpcStartInteractingOnClient( mast.gameObject, gameObject, isLeft, indexOfClosest );
					mast.indexOfFirstGrabbed = indexOfClosest;

					rightHandInteracting = true;
				}
			}
		}
	}

	[ClientRpc]
	private void RpcStartInteractingOnClient( GameObject mastTrigger,GameObject player, bool isLeft, int iOfClosest ) {
		if (player != gameObject && !isServer) {
            ////print("not this go");
			return;
		}

        foreach (var node in mastTrigger.GetComponentInChildren<MastAngleSetterTrigger>().nodes) {
            node.SetActive(false);
        }
        foreach ( MastAimNode node in mastTrigger.GetComponentsInChildren<MastAimNode>() ) {
			node.particles.SetActive( true);
		}

		mast = mastTrigger.GetComponent<MastSwitch>();
		indexOfClosest = iOfClosest;

		if (isLeft) {
			leftHandInteracting = true;
		} else {
			rightHandInteracting = true;
		}
	}

	[ClientRpc]
	private void RpcStopInteractingOnClient( GameObject mastTrigger, GameObject player, bool isLeft, bool showMarkerNodes ) {
		if ( player != gameObject && !isServer ) {
            ////print("RPC stop interacting called but returned");

            return;
		}

        ////print("RPC stop interacting called");


        foreach ( MastAimNode node in mastTrigger.GetComponentsInChildren<MastAimNode>() ) {
			node.particles.SetActive( false );
		}

        mastTrigger.GetComponentInChildren<MastAngleSetterTrigger>().TurnOffNodes();

        mast = null;
		indexOfClosest = -1;
		if ( isLeft ) {
			leftHandInteracting = false;
		} else {
			rightHandInteracting = false;
		}
	}

	[Command]
	public void CmdStopInteracting(bool isLeft, bool showMarkerNodes) {
        //print("stop interacting called");
		for ( int index = 0; index < mast.aimingNodes.Length; index++ ) {
			Transform node = mast.aimingNodes[index].transform;
			//node.GetComponent<Renderer>().enabled = false;
			node.GetComponent<MastAimNode>().player = null;
		}

		RpcStopInteractingOnClient( mast.gameObject, gameObject, isLeft, showMarkerNodes );
		mast.indexOfFirstGrabbed = -1;
		mast = null;
		indexOfClosest = -1;
	}

    [ClientRpc]
    public void RpcTurnOffHintNodes(GameObject mast) {
        mast.GetComponentInChildren<MastAngleSetterTrigger>().TurnOffNodes();
    }

    [ClientRpc]
    public void RpcTurnONHintNodes(GameObject mast) {
        mast.GetComponentInChildren<MastAngleSetterTrigger>().TurnONNodes();
    }

}
