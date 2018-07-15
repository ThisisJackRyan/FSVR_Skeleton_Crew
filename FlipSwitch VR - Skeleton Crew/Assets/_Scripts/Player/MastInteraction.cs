using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MastInteraction : NetworkBehaviour {

	public bool emptyLeftHand;
	public bool emptyRightHand;
	public float radius = 0.1f;
	public GameObject grabPoint, handPoint;
	public Transform rightHand, leftHand;
	public bool leftHandInteracting;
	public bool rightHandInteracting;
	public MastSwitch mast;
	public GameObject gp;
	public GameObject hp;
	public GameObject rp;

	//internal GameObject selected;

	private void Start() {
		emptyLeftHand = true;
		emptyRightHand = true;
		mast = FindObjectOfType<MastSwitch>();		
	}

	private void Update() {
		if ( !isLocalPlayer )
			return;

		if(emptyLeftHand && Controller.LeftController.GetPressDown( Controller.Grip ) ) {
			RaycastHit[] hits = Physics.SphereCastAll( leftHand.position, radius, leftHand.forward );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "MastRope" ) {
					print( "mast grab " + hits[i].point );
					gp = Instantiate( grabPoint, leftHand.position, Quaternion.identity );
					hp = Instantiate( handPoint, leftHand.position, Quaternion.identity );
					hp.GetComponent<HeightMatch>().toMatch = leftHand;
					hp.GetComponent<HeightMatch>().mastInteraction = this;
					rp = Instantiate( grabPoint, leftHand.position - new Vector3( 0, .5f, 0 ), Quaternion.identity );
					rp.tag = "ReleasePoint";
					print( rp.tag );

					leftHandInteracting = true;
				}
			}
		}

		if(emptyRightHand && Controller.RightController.GetPressDown( Controller.Grip ) ) {
			RaycastHit[] hits = Physics.SphereCastAll( rightHand.position, radius, rightHand.forward );
			for ( int i = 0; i < hits.Length; i++ ) {
				if ( hits[i].transform.tag == "MastRope" ) {
					print( "mast grab " + hits[i].point );
					gp = Instantiate( grabPoint, rightHand.position, Quaternion.identity );
					hp = Instantiate( handPoint, rightHand.position, Quaternion.identity );
					hp.GetComponent<HeightMatch>().toMatch = rightHand;
					hp.GetComponent<HeightMatch>().mastInteraction = this;
					rp = Instantiate( grabPoint, rightHand.position - new Vector3( 0, .5f, 0 ), Quaternion.identity );
					rp.tag = "ReleasePoint";
					print( rp.tag );

					rightHandInteracting = true;
				}
			}
		}

		if(gp != null && hp != null && leftHandInteracting && Controller.LeftController.GetPressUp( Controller.Grip )) {
			CleanupPoints();
			//leftHandInteracting = false;
		}

		if ( gp != null && hp != null && rightHandInteracting && Controller.RightController.GetPressUp( Controller.Grip )) {
			CleanupPoints();
			//rightHandInteracting = false;
		}
	}

	[Command]
	public void CmdReachedTarget() {
		mast.SwapMode();
		RpcReachedTarget();
        Captain.instance.MastHasBeenPulled();
	}
	
	[ClientRpc]
	private void RpcReachedTarget() {
		if (isServer) {
			return;
		}

		print( "rpc reached target" );
		mast.SwapMode();
		if ( isLocalPlayer ) {
			Debug.LogWarning( "is local player on " + name );
			CleanupPoints();
		}
	}


	public void CleanupPoints() {
		Destroy( gp );
		Destroy( hp );
		Destroy( rp );
		rightHandInteracting = leftHandInteracting = false;
	}

}
