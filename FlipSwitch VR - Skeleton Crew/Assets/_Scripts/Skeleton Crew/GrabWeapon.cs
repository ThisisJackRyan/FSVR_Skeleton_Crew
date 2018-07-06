using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabWeapon : NetworkBehaviour {

	public Transform rightHand, leftHand;
	public float radius = 0.1f;

	private bool leftHandIsGrabbing = false;
	private bool rightHandIsGrabbing = false;
	private WeaponInteraction weaponInteraction;
	GameObject leftWeaponBeingHeld;
	GameObject rightWeaponBeingHeld;

	// Use this for initialization
	void Start() {
		weaponInteraction = GetComponent<WeaponInteraction>();
	}

	// Update is called once per frame
	void Update() {
		if ( !isLocalPlayer ) {
			return;
		}

		if (Controller.RightController.GetPressDown(Controller.Grip) ) {
			CmdToggleHand("right");
		} 

		if ( Controller.LeftController.GetPressDown(Controller.Grip ) ) {
			CmdToggleHand( "left" );
		} 
	}

	[Command]
	private void CmdToggleHand(string side) {

		print( gameObject.name + ": toggling the hand on " + side + " side" );
		if ( side.Equals( "right" ) ) {
			rightHandIsGrabbing = !rightHandIsGrabbing;
			if ( rightHandIsGrabbing )
				HandleGrabbing( "right" );
			else
				HandleDropping( "right" );
		} else {
			leftHandIsGrabbing = !leftHandIsGrabbing;
			if ( leftHandIsGrabbing )
				HandleGrabbing( "left" );
			else
				HandleDropping( "left" );
		}

		RpcToggleHand(side);
	}

	[ClientRpc]
	private void RpcToggleHand(string side) {
		if ( isServer )
			return;

		print( gameObject.name + ": toggling the hand on " + side + " side" );
		if ( side.Equals( "right" ) ) {
			rightHandIsGrabbing = !rightHandIsGrabbing;
			if ( rightHandIsGrabbing )
				HandleGrabbing( "right" );
			else
				HandleDropping( "right" );
		} else {
			leftHandIsGrabbing = !leftHandIsGrabbing;
			if ( leftHandIsGrabbing )
				HandleGrabbing( "left" );
			else
				HandleDropping( "left" );
		}
	}

	void HandleDropping(string side)
	{
		if (side.Equals("left")) {
			RaycastHit[] hits = Physics.SphereCastAll( leftHand.position, radius, transform.forward );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {

				for ( int i = 0; i < hits.Length; i++ ) {

					if ( !leftWeaponBeingHeld ) {
						return;
					} else {
						//something in hand, handle holster/drop
						//has kids

						needsToDrop = true;
						leftWeaponBeingHeld.GetComponent<Weapon>().TurnOffFire();

						if ( hits[i].transform.tag == "Holster" ) {
							var temp = leftWeaponBeingHeld;
                            
							temp.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
							temp.GetComponent<ObjectPositionLock>().posOffset = temp.transform.GetComponent<Weapon>().data.holsteredPosition;
							temp.GetComponent<ObjectPositionLock>().rotOffset = temp.transform.GetComponent<Weapon>().data.holsteredRotation;
							temp.transform.GetComponent<Rigidbody>().isKinematic = true;
							print( gameObject.name + ": holstering " + leftWeaponBeingHeld.name + " on the left side" );
							leftWeaponBeingHeld = null;
							needsToDrop = false;
							

							weaponInteraction.UnassignWeapon( side );
							return;
						}

						//no holsters found, drop weapon
					}
				}

			}

			if ( needsToDrop && leftWeaponBeingHeld ) {

				print( gameObject.name + ": dropping left hand weapon: " + leftWeaponBeingHeld.name );

				leftWeaponBeingHeld.GetComponent<ObjectPositionLock>().posPoint = null;
				leftWeaponBeingHeld.GetComponent<Rigidbody>().isKinematic = false;
				leftWeaponBeingHeld = null;
				weaponInteraction.UnassignWeapon( side );
			}
			
		} else {
			RaycastHit[] hits = Physics.SphereCastAll( rightHand.position, radius, transform.forward );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {

				for ( int i = 0; i < hits.Length; i++ ) {

					if ( !rightWeaponBeingHeld ) {
						return;
					} else {

						needsToDrop = true;
						rightWeaponBeingHeld.GetComponent<Weapon>().TurnOffFire();

						if ( hits[i].transform.tag == "Holster" ) {
							//var temp = hand.GetChild( 0 );
							var temp = rightWeaponBeingHeld;

							//temp.transform.SetParent( hits[i].transform );
							temp.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
							temp.GetComponent<ObjectPositionLock>().posOffset = temp.transform.GetComponent<Weapon>().data.holsteredPosition;
							temp.GetComponent<ObjectPositionLock>().rotOffset = temp.transform.GetComponent<Weapon>().data.holsteredRotation;
							temp.transform.GetComponent<Rigidbody>().isKinematic = true;

							print( gameObject.name + ": holstering " + rightWeaponBeingHeld.name + " on the right side" );
							rightWeaponBeingHeld = null;
							needsToDrop = false;


							weaponInteraction.UnassignWeapon( side );
							return;
						}

						//no holsters found, drop weapon
					}
				}

			}

			if ( needsToDrop && rightWeaponBeingHeld ) {

				print( gameObject.name + ": dropping right hand weapon: " + rightWeaponBeingHeld.name );

				rightWeaponBeingHeld.GetComponent<ObjectPositionLock>().posPoint = null;
				rightWeaponBeingHeld.GetComponent<Rigidbody>().isKinematic = false;
		
				rightWeaponBeingHeld = null;
				weaponInteraction.UnassignWeapon( side );
			}
		}

	   
	}


	void HandleGrabbing(string side)
	{
		Transform hand;
		SteamVR_Controller.Device con;
		if (side.Equals("left"))
		{
			hand = leftHand;
			if(isLocalPlayer)
				con = Controller.LeftController;
		}
		else
		{
			hand = rightHand;
			if(isLocalPlayer)
				con = Controller.RightController;
		}

		RaycastHit[] hits = Physics.SphereCastAll(hand.position, radius, transform.forward);

		if (hits.Length > 0)
		{
			//print("hits > 0 with " + hits.Length);
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].transform.tag == "Weapon") {

					GameObject temp;

					if ( side.Equals( "left" ) ) {
						temp = leftWeaponBeingHeld = hits[i].transform.gameObject;
						print(gameObject.name + ": setting left hand weapon being held to " + hits[i].transform.gameObject.name );
					} else {
						temp = rightWeaponBeingHeld = hits[i].transform.gameObject;
						print( gameObject.name + ": setting right hand weapon being held to " + hits[i].transform.gameObject.name );
					}
					
					hits[i].transform.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
					hits[i].transform.GetComponent<ObjectPositionLock>().posOffset = hits[i].transform.GetComponent<Weapon>().data.heldPosition;
					hits[i].transform.GetComponent<ObjectPositionLock>().rotOffset = hits[i].transform.GetComponent<Weapon>().data.heldRotation;

					hits[i].transform.GetComponent<Rigidbody>().isKinematic = true;

					weaponInteraction.AssignWeapon( side, temp );
					return;
				} else {
					print( gameObject.name + ": didn't hit a weapon. hit " + hits[i].transform.name + " instead." );
				}

			}
		}
	}

	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere( rightHand.transform.position, radius );
		Gizmos.DrawSphere( leftHand.transform.position, radius );
	}

}
