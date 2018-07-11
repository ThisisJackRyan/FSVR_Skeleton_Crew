using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabWeapon : NetworkBehaviour {
	public Transform rightHand, leftHand;
	public GameObject rightHolster, leftHolster;
	public float radius = 0.1f;
	private WeaponInteraction weaponInteraction;
	private GameObject leftWeaponGameObj;
	private GameObject rightWeaponGameObj;
	private GameObject leftHighlightedWeaponObj;
	private GameObject rightHighlightedWeaponObj;

	#region syncVar

	#endregion

	// Use this for initialization
	void Start() {
		weaponInteraction = GetComponent<WeaponInteraction>();
	}

	void Update() {
		if (!isLocalPlayer) {
			return;
		}

        if (rightWeaponGameObj)
        {
            if (Controller.RightController.GetPressDown(Controller.Grip))
            {
                CmdDropIfHolding("right");
                //run highlight logic here
            }
        } else {
		    if (Controller.RightController.GetPress(Controller.Grip)) {
				//run highlight logic here
				CmdFindAndHighlightNearestWeapon("right", gameObject);
			}

            if (Controller.RightController.GetPressUp(Controller.Grip))
            {
                CmdGrabIfHighlighted("right");
                //run highlight logic here
            }
        }

        if (leftWeaponGameObj) {
            if (Controller.LeftController.GetPressDown(Controller.Grip))
            {
                CmdDropIfHolding("left");
                //run highlight logic here
            }
        } else {
		    if (Controller.LeftController.GetPress(Controller.Grip)) {
				//run highlight logic here
				CmdFindAndHighlightNearestWeapon("left", gameObject);
			}
            if (Controller.LeftController.GetPressUp(Controller.Grip)) {
			CmdGrabIfHighlighted("left");
			//run highlight logic here
		    }
		}
        
	}

    #region Commands
	[Command]
	private void CmdFindAndHighlightNearestWeapon(string side, GameObject player) {
		Transform hand = side.Equals("left") ? leftHand : rightHand;

		RaycastHit[] hits = Physics.SphereCastAll(hand.position, radius, hand.forward);
		GameObject closest = null;
		bool hitWeapon = false;

		if (hits.Length > 0) {
			//print("hits > 0 with " + hits.Length);
			for (int i = 0; i < hits.Length; i++) {
				if (hits[i].transform.tag == "Weapon") {
                    if (hits[i].transform.GetComponent<Weapon>().isBeingHeldByPlayer){
                        continue;
                    }

					hitWeapon = true;
					if (!closest) {
						closest = hits[i].transform.gameObject;
					}
					else {
						if (Mathf.Abs(Vector3.Distance(hand.position, hits[i].transform.position)) <
						    Mathf.Abs(Vector3.Distance(hand.position, closest.transform.position))) {
							//print("wjbwefkjgb");
							closest = hits[i].transform.gameObject;
						}
					}
				}
			}
		}

		if (closest) {
			if (Mathf.Abs(Vector3.Distance(closest.transform.position, hand.position)) >= radius  || hitWeapon == false) {
				print("no weapon in range");
				closest = null;

				if (side.Equals("left")) {
					leftHighlightedWeaponObj = null;
				}
				else if (side.Equals("right")) {
					rightHighlightedWeaponObj = null;
				}

				RpcUnhighlightWeapon(side, player);
			} else {
				if (side.Equals("left")) {
					leftHighlightedWeaponObj = closest;
				}
				else if (side.Equals("right")) {
					rightHighlightedWeaponObj = closest;
				}

				RpcHighlightWeapon(side, closest, player);
			}
		}
	}

    [Command]
	private void CmdGrabIfHighlighted(string side) {
		Transform hand = side.Equals("left") ? leftHand : rightHand;
		GameObject temp = null;

		if (side.Equals("left")) {
			if (leftHighlightedWeaponObj) {
			    temp = leftWeaponGameObj = leftHighlightedWeaponObj;
				leftHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
				leftHighlightedWeaponObj = null;
			}
		}
		else {
			if (rightHighlightedWeaponObj) {
			    temp = rightWeaponGameObj = rightHighlightedWeaponObj;
				rightHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
				rightHighlightedWeaponObj = null;
			}
		}

		if (temp != null) {
            //keep?
            if (Mathf.Abs(Vector3.Distance(temp.transform.position, hand.position)) >= radius) {
                RpcUnhighlightWeapon(side, gameObject);

                if (side.Equals("left")) {
                    leftWeaponGameObj = null;
                }
                else
                {
                    rightWeaponGameObj = null;
                }
                return;
            }

			temp.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
			temp.GetComponent<ObjectPositionLock>().posOffset = temp.GetComponent<Weapon>().data.heldPosition;
			temp.GetComponent<ObjectPositionLock>().rotOffset = temp.GetComponent<Weapon>().data.heldRotation;
            temp.GetComponent<Weapon>().isBeingHeldByPlayer = true;

			temp.GetComponent<Rigidbody>().isKinematic = true;

			weaponInteraction.AssignWeapon(side, temp);
			RpcGrabWeapon(side, temp);
		}
	}

	[Command]
	private void CmdDropIfHolding(string side) {
		if (side.Equals("right")) {
			print("calls handle dropping right in cmd " + rightWeaponGameObj);

			if (rightWeaponGameObj != null) {
                print("right weapon exist, drop it");
				HandleDropping("right");
			}
		} else {
			print("calls handle dropping left in cmd " + leftWeaponGameObj);

			if (leftWeaponGameObj != null) {
                print("left weapon exist, drop it");

                HandleDropping("left");
			}
		}
	}

    #endregion

    #region RPC

    [ClientRpc]
	private void RpcUnhighlightWeapon(string side, GameObject player) {
		if (isServer)
			return;

		if (isLocalPlayer && player == gameObject) {
			if (side.Equals("left")) {
				if (leftHighlightedWeaponObj) {
					leftHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
					leftHighlightedWeaponObj = null;
				}
			}
			else {
				if (rightHighlightedWeaponObj) {
					rightHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
					rightHighlightedWeaponObj = null;
				}
			}
		}
	}

	[ClientRpc]
	private void RpcHighlightWeapon(string side, GameObject weaponToHighlight, GameObject player) {
		if (isServer) {
			return;
		}

		if (isLocalPlayer && player == gameObject) {
			if (side.Equals("left")) {
				if (leftHighlightedWeaponObj) {
					leftHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
				}

				weaponToHighlight.GetComponent<Weapon>().myOutline.enabled = true;
				leftHighlightedWeaponObj = weaponToHighlight;
			}
			else {
				if (rightHighlightedWeaponObj) {
					rightHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
				}

				weaponToHighlight.GetComponent<Weapon>().myOutline.enabled = true;
				rightHighlightedWeaponObj = weaponToHighlight;
			}
		}
	}

	[ClientRpc]
	private void RpcHolster(string side, GameObject holster) {
		if (isServer)
			return;

		print("rpc holster called on client");

		if (side.Equals("left")) {
			leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster.gameObject;
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
			leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
		}
		else {
			rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster.gameObject;
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset =
				rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset =
				rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
			rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
		}

		weaponInteraction.UnassignWeapon(side);
	}

	[ClientRpc]
	private void RpcDropWeapon(string side) {
		if (isServer)
			return;

		print("rpc drop weapon called on client");

		if (side.Equals("left")) {
			leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
            leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
			leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
			leftWeaponGameObj = null;
			weaponInteraction.UnassignWeapon(side);
		}
		else {
			rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
            rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
			rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
			rightWeaponGameObj = null;
			weaponInteraction.UnassignWeapon(side);
		}
	}

	[ClientRpc]
	private void RpcGrabWeapon(string side, GameObject weapon) {
		if (isServer) {
			return;
		}

		Transform hand;
		if (side.Equals("left")) {
			leftWeaponGameObj = weapon;
			hand = leftHand;
			print(gameObject.name + ": setting left hand weapon being held to " + weapon.name);

			leftHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
			leftHighlightedWeaponObj = null;
		}
		else {
			rightWeaponGameObj = weapon;
			hand = rightHand;
			print(gameObject.name + ": setting right hand weapon being held to " + weapon.name);

			rightHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
			rightHighlightedWeaponObj = null;
		}

		weapon.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
		weapon.GetComponent<ObjectPositionLock>().posOffset = weapon.GetComponent<Weapon>().data.heldPosition;
		weapon.GetComponent<ObjectPositionLock>().rotOffset = weapon.GetComponent<Weapon>().data.heldRotation;
        weapon.GetComponent<Weapon>().isBeingHeldByPlayer = true;


        weapon.GetComponent<Rigidbody>().isKinematic = true;

		weaponInteraction.AssignWeapon(side, weapon);
	}

    #endregion

    void HandleDropping(string side) {
		Debug.LogWarning("drop called for " + side);

		if (side.Equals("left")) {
			RaycastHit[] hits = Physics.SphereCastAll(leftHand.position, radius, transform.forward);

			bool needsToDrop = false;

			if (hits.Length > 0) {
				Debug.LogWarning("left hits length of " + hits.Length);

				for (int i = 0; i < hits.Length; i++) {
					needsToDrop = true;
					leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

					if (hits[i].transform.tag == "Holster") {
						print("finds holster left on server");
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
						print(gameObject.name + ": holstering " + leftWeaponGameObj.name + " on the left side");
						leftWeaponGameObj = null;

						weaponInteraction.UnassignWeapon(side);

						RpcHolster(side, hits[i].transform.gameObject);
						return;
					}

					//no holsters found, drop weapon
				}
			}


			if (needsToDrop && leftWeaponGameObj) {
				print("gets to drop weapon left on server");
				print(gameObject.name + ": dropping left hand weapon: " + leftWeaponGameObj.name);
				leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
                leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;

                leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
				leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
				leftWeaponGameObj = null;
				weaponInteraction.UnassignWeapon(side);

				RpcDropWeapon(side);
			}
		}
		else {
			RaycastHit[] hits = Physics.SphereCastAll(rightHand.position, radius, transform.forward);

			bool needsToDrop = false;

			if (hits.Length > 0) {
				Debug.LogWarning("right hits length of " + hits.Length);
				for (int i = 0; i < hits.Length; i++) {
					needsToDrop = true;
					rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

					if (hits[i].transform.tag == "Holster") {
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset =
							rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset =
							rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;

						print(gameObject.name + ": holstering " + rightWeaponGameObj.name + " on the right side");
						rightWeaponGameObj = null;


						weaponInteraction.UnassignWeapon(side);

						RpcHolster(side, rightWeaponGameObj);
						return;
					}

					//no holsters found, drop weapon
				}
			}

			if (needsToDrop && rightWeaponGameObj) {
				print(gameObject.name + ": dropping right hand weapon: " + rightWeaponGameObj.name);
				rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
                rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
				rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;

				rightWeaponGameObj = null;
				weaponInteraction.UnassignWeapon(side);

				RpcDropWeapon(side);
			}
		}
	}

	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere(rightHand.transform.position, radius);
		Gizmos.DrawSphere(leftHand.transform.position, radius);
	}
}