using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabWeapon : NetworkBehaviour {

	public Transform rightHand, leftHand;
    public GameObject rightHolster, leftHolster;
	public float radius = 0.1f;

	private bool leftHandIsGrabbing = false;
	private bool rightHandIsGrabbing = false;
	private WeaponInteraction weaponInteraction;
	private GameObject leftWeapon;
    private GameObject rightWeapon;

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
    }

    [ClientRpc]
    private void RpcHolster(string side, GameObject holster)
    {
        if (isServer)
            return;

        if (side.Equals("left")) {

            leftWeapon.GetComponent<Weapon>().TurnOffFire();
            leftWeapon.GetComponent<ObjectPositionLock>().posPoint = holster.gameObject;
            leftWeapon.GetComponent<ObjectPositionLock>().posOffset = leftWeapon.GetComponent<Weapon>().data.holsteredPosition;
            leftWeapon.GetComponent<ObjectPositionLock>().rotOffset = leftWeapon.GetComponent<Weapon>().data.holsteredRotation;
            leftWeapon.GetComponent<Rigidbody>().isKinematic = true;
        } else {

            rightWeapon.GetComponent<Weapon>().TurnOffFire();
            rightWeapon.GetComponent<ObjectPositionLock>().posPoint = holster.gameObject;
            rightWeapon.GetComponent<ObjectPositionLock>().posOffset = rightWeapon.GetComponent<Weapon>().data.holsteredPosition;
            rightWeapon.GetComponent<ObjectPositionLock>().rotOffset = rightWeapon.GetComponent<Weapon>().data.holsteredRotation;
            rightWeapon.GetComponent<Rigidbody>().isKinematic = true;
        }        

        weaponInteraction.UnassignWeapon(side);
    }

    [ClientRpc]
    private void RpcDropWeapon(string side)
    {
        if (isServer)
            return;

        if (side.Equals("left")) {

            leftWeapon.GetComponent<Weapon>().TurnOffFire();
            leftWeapon.GetComponent<ObjectPositionLock>().posPoint = null;
            leftWeapon.GetComponent<Rigidbody>().isKinematic = false;
            leftWeapon = null;
            weaponInteraction.UnassignWeapon(side);
        } else {

            rightWeapon.GetComponent<Weapon>().TurnOffFire();
            rightWeapon.GetComponent<ObjectPositionLock>().posPoint = null;
            rightWeapon.GetComponent<Rigidbody>().isKinematic = false;
            rightWeapon = null;
            weaponInteraction.UnassignWeapon(side);
        }
    }

	void HandleDropping(string side)
	{
		if (side.Equals("left")) {
			RaycastHit[] hits = Physics.SphereCastAll( leftHand.position, radius, transform.forward );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {

				for ( int i = 0; i < hits.Length; i++ ) {

					if ( !leftHandIsGrabbing ) {
						return;
					} else {

						needsToDrop = true;
						leftWeapon.GetComponent<Weapon>().TurnOffFire();

						if ( hits[i].transform.tag == "Holster" ) {

                            leftWeapon.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                            leftWeapon.GetComponent<ObjectPositionLock>().posOffset = leftWeapon.GetComponent<Weapon>().data.holsteredPosition;
                            leftWeapon.GetComponent<ObjectPositionLock>().rotOffset = leftWeapon.GetComponent<Weapon>().data.holsteredRotation;
                            leftWeapon.GetComponent<Rigidbody>().isKinematic = true;
							print( gameObject.name + ": holstering " + leftWeapon.name + " on the left side" );
                            leftWeapon = null;
							needsToDrop = false;

                            weaponInteraction.UnassignWeapon( side );

                            RpcHolster(side, hits[i].transform.gameObject);
							return;
						}

						//no holsters found, drop weapon
					}
				}

			}

			if ( needsToDrop && leftHandIsGrabbing ) {

				print( gameObject.name + ": dropping left hand weapon: " + leftWeapon.name );
                leftWeapon.GetComponent<Weapon>().TurnOffFire();
                leftWeapon.GetComponent<ObjectPositionLock>().posPoint = null;
                leftWeapon.GetComponent<Rigidbody>().isKinematic = false;
                leftWeapon = null;
				weaponInteraction.UnassignWeapon( side );

                RpcDropWeapon(side);
			}
			
		} else {
			RaycastHit[] hits = Physics.SphereCastAll( rightHand.position, radius, transform.forward );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {

				for ( int i = 0; i < hits.Length; i++ ) {

					if ( !rightHandIsGrabbing ) {
						return;
					} else {

						needsToDrop = true;
						rightWeapon.GetComponent<Weapon>().TurnOffFire();

						if ( hits[i].transform.tag == "Holster" ) {

                            rightWeapon.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                            rightWeapon.GetComponent<ObjectPositionLock>().posOffset = rightWeapon.GetComponent<Weapon>().data.holsteredPosition;
                            rightWeapon.GetComponent<ObjectPositionLock>().rotOffset = rightWeapon.GetComponent<Weapon>().data.holsteredRotation;
                            rightWeapon.GetComponent<Rigidbody>().isKinematic = true;

							print( gameObject.name + ": holstering " + rightWeapon.name + " on the right side" );
                            rightWeapon = null;
							needsToDrop = false;


							weaponInteraction.UnassignWeapon( side );

                            RpcHolster(side, rightWeapon);
							return;
						}

						//no holsters found, drop weapon
					}
				}

			}

			if ( needsToDrop && rightHandIsGrabbing ) {

				print( gameObject.name + ": dropping right hand weapon: " + rightWeapon.name );
                rightWeapon.GetComponent<Weapon>().TurnOffFire();
                rightWeapon.GetComponent<ObjectPositionLock>().posPoint = null;
                rightWeapon.GetComponent<Rigidbody>().isKinematic = false;

                rightWeapon = null;
				weaponInteraction.UnassignWeapon( side );

                RpcDropWeapon(side);
			}
		}	   
	}


	void HandleGrabbing(string side)
	{
		Transform hand = side.Equals("left") ? leftHand : rightHand;

		RaycastHit[] hits = Physics.SphereCastAll(hand.position, radius, transform.forward);

		if (hits.Length > 0)
		{
			//print("hits > 0 with " + hits.Length);
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].transform.tag == "Weapon") {

					GameObject temp;

					if ( side.Equals( "left" ) ) {
						temp = leftWeapon = hits[i].transform.gameObject;
						print(gameObject.name + ": setting left hand weapon being held to " + temp.name );
					} else {
						temp = rightWeapon = hits[i].transform.gameObject;
						print( gameObject.name + ": setting right hand weapon being held to " + temp.name );
					}

                    temp.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
                    temp.GetComponent<ObjectPositionLock>().posOffset = temp.GetComponent<Weapon>().data.heldPosition;
                    temp.GetComponent<ObjectPositionLock>().rotOffset = temp.GetComponent<Weapon>().data.heldRotation;

					temp.GetComponent<Rigidbody>().isKinematic = true;

					weaponInteraction.AssignWeapon( side, temp );
                    RpcGrabWeapon(side, temp);
					return;
				} 
			}
		}
	}

    [ClientRpc]
    private void RpcGrabWeapon(string side, GameObject weapon) {

        if (isServer)
            return;

        Transform hand;
        if (side.Equals("left"))
        {
            leftWeapon = weapon;
            hand = leftHand;
            print(gameObject.name + ": setting left hand weapon being held to " + weapon.name);
        }
        else
        {
            rightWeapon = weapon;
            hand = rightHand;
            print(gameObject.name + ": setting right hand weapon being held to " + weapon.name);
        }

        weapon.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
        weapon.GetComponent<ObjectPositionLock>().posOffset = weapon.GetComponent<Weapon>().data.heldPosition;
        weapon.GetComponent<ObjectPositionLock>().rotOffset = weapon.GetComponent<Weapon>().data.heldRotation;

        weapon.GetComponent<Rigidbody>().isKinematic = true;

        weaponInteraction.AssignWeapon(side, weapon);
    }

	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere( rightHand.transform.position, radius );
		Gizmos.DrawSphere( leftHand.transform.position, radius );
	}

}
