using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(WeaponInteraction))]
public class GrabWeapon : NetworkBehaviour {
    private WeaponInteraction weaponInteraction;
    public float radius = 0.1f;

    public Transform rightHand, leftHand;
    public GameObject rightHolster, leftHolster;
    private GameObject weaponInRightHolster, weaponInLeftHolster;

    private GameObject leftWeaponGameObj;
    private GameObject rightWeaponGameObj;
    private GameObject leftHighlightedWeaponObj;
    private GameObject rightHighlightedWeaponObj;

    // Use this for initialization
    void Start() {
        weaponInteraction = GetComponent<WeaponInteraction>();
    }

    void Update() {
        if (!isLocalPlayer) {
            return;
        }

        if (rightWeaponGameObj) {
            if (Controller.RightController.GetPressDown(Controller.Grip)) {
                CmdDropIfHolding("right", gameObject);
                //run highlight logic here
            }
        } else {
            if (Controller.RightController.GetPress(Controller.Grip)) {
                //run highlight logic here
                CmdFindAndHighlightNearestWeapon("right", gameObject);
            }

            if (Controller.RightController.GetPressUp(Controller.Grip)) {
                CmdGrabIfHighlighted("right");
                //run highlight logic here
            }
        }

        if (leftWeaponGameObj) {
            if (Controller.LeftController.GetPressDown(Controller.Grip)) {
                CmdDropIfHolding("left", gameObject);
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
            ////print("hits > 0 with " + hits.Length);
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].transform.tag == "Weapon") {
                    if (hits[i].transform.GetComponent<Weapon>().isBeingHeldByPlayer) {
                        continue;
                    }

                    if (hits[i].transform.GetComponent<Weapon>().playerWhoHolstered != player && hits[i].transform.GetComponent<Weapon>().playerWhoHolstered != null) {
                            continue;                        
                    }

                    hitWeapon = true;
                    if (!closest) {
                        closest = hits[i].transform.gameObject;
                    } else {
                        if (Mathf.Abs(Vector3.Distance(hand.position, hits[i].transform.position)) <
                            Mathf.Abs(Vector3.Distance(hand.position, closest.transform.position))) {
                            ////print("wjbwefkjgb");
                            closest = hits[i].transform.gameObject;
                        }
                    }
                }
            }
        }

        if (closest) {
            if (Mathf.Abs(Vector3.Distance(closest.transform.position, hand.position)) >= radius || hitWeapon == false) {
                //print("no weapon in range");
                closest = null;

                if (side.Equals("left")) {
                    leftHighlightedWeaponObj = null;
                } else if (side.Equals("right")) {
                    rightHighlightedWeaponObj = null;
                }

                RpcUnhighlightWeapon(side, player);
            } else {
                if (side.Equals("left")) {
                    leftHighlightedWeaponObj = closest;
                } else if (side.Equals("right")) {
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
        } else {
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
                } else {
                    rightWeaponGameObj = null;
                }
                return;
            }

            temp.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
            temp.GetComponent<ObjectPositionLock>().posOffset = temp.GetComponent<Weapon>().data.heldPosition;
            temp.GetComponent<ObjectPositionLock>().rotOffset = temp.GetComponent<Weapon>().data.heldRotation;
            temp.GetComponent<Weapon>().isBeingHeldByPlayer = true;
            temp.GetComponent<Weapon>().playerWhoHolstered = null;


            temp.GetComponent<Rigidbody>().isKinematic = true;

            if (temp == weaponInRightHolster) {
                //print("found weapon in right holster");
                weaponInRightHolster = null;
            } else if (temp == weaponInLeftHolster) {
                //print("found weapon in left holster");

                weaponInLeftHolster = null;
            }

            weaponInteraction.AssignWeapon(side, temp);
            RpcGrabWeapon(side, temp);
        }
    }

    [Command]
    private void CmdDropIfHolding(string side, GameObject player) {
        if (side.Equals("right")) {
            //print("calls handle dropping right in cmd " + rightWeaponGameObj);

            if (rightWeaponGameObj != null) {
                //print("right weapon exist, drop it");
                HandleDropping("right", player);
            }
        } else {
            //print("calls handle dropping left in cmd " + leftWeaponGameObj);

            if (leftWeaponGameObj != null) {
                //print("left weapon exist, drop it");

                HandleDropping("left", player);
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
            } else {
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
            } else {
                if (rightHighlightedWeaponObj) {
                    rightHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
                }

                weaponToHighlight.GetComponent<Weapon>().myOutline.enabled = true;
                rightHighlightedWeaponObj = weaponToHighlight;
            }
        } else if(player == gameObject) {
            if (side.Equals("left")) {
                leftHighlightedWeaponObj = weaponToHighlight;
            } else {
                rightHighlightedWeaponObj = weaponToHighlight;
            }
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
            ////print(gameObject.name + ": setting left hand weapon being held to " + weapon.name);

            leftHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
            leftHighlightedWeaponObj = null;
        } else {
            rightWeaponGameObj = weapon;
            hand = rightHand;
            ////print(gameObject.name + ": setting right hand weapon being held to " + weapon.name);

            rightHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
            rightHighlightedWeaponObj = null;
        }

        weapon.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
        weapon.GetComponent<ObjectPositionLock>().posOffset = weapon.GetComponent<Weapon>().data.heldPosition;
        weapon.GetComponent<ObjectPositionLock>().rotOffset = weapon.GetComponent<Weapon>().data.heldRotation;
        weapon.GetComponent<Weapon>().isBeingHeldByPlayer = true;
        weapon.GetComponent<Weapon>().playerWhoHolstered = null;


        weapon.GetComponent<Rigidbody>().isKinematic = true;

        weaponInteraction.AssignWeapon(side, weapon);
    }

    [ClientRpc]
    private void RpcDropWeapon(string side) {
        if (isServer)
            return;

        //print("rpc drop weapon called on client");

        if (side.Equals("left")) {
            leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
            leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

            leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
            leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
            leftWeaponGameObj = null;
            weaponInteraction.UnassignWeapon(side);
        } else {
            rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
            rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

            rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
            rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
            rightWeaponGameObj = null;
            weaponInteraction.UnassignWeapon(side);
        }
    }

    [ClientRpc]
    private void RpcHolster(string side, bool holsterLeft) {
        if (isServer)
            return;

        //print("rpc holster called on client");

        GameObject holster = (holsterLeft) ? leftHolster : rightHolster;

        if (side.Equals("left")) {
            //print("rpc side is left and the weapon is " + leftWeaponGameObj);
            leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster;
            leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
            leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
            leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;

            if (holster == rightHolster) {
                //print("rpc side is left with right holster");
                weaponInRightHolster = leftWeaponGameObj;
            } else if (holster == leftHolster) {
                //print("rpc side is left with left holster");
                weaponInLeftHolster = leftWeaponGameObj;
            }

            leftWeaponGameObj = null;
        } else {
            //print("rpc side is right and the weapon is " + rightWeaponGameObj);

            rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
            rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster;
            rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
            rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
            rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;

            //print("holster is " + holster.name);
            //print("right holster is " + rightHolster.name);

            if (holster == rightHolster) {
                //print("rpc side is right with right holster");
                weaponInRightHolster = rightWeaponGameObj;
            } else if (holster == leftHolster) {
                //print("rpc side is right with left holster");
                weaponInLeftHolster = rightWeaponGameObj;
            }

            rightWeaponGameObj = null;
        }

        weaponInteraction.UnassignWeapon(side);
    }

    #endregion

    void HandleDropping(string side, GameObject player) {
        Debug.LogWarning("drop called for " + side);

        if (side.Equals("left")) {
            RaycastHit[] hits = Physics.SphereCastAll(leftHand.position, radius, transform.forward);

            bool needsToDrop = false;

            if (hits.Length > 0) {
                Debug.LogWarning("left hits length of " + hits.Length);

                for (int i = 0; i < hits.Length; i++) {
                    needsToDrop = true;
                    leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

                    if (hits[i].transform == rightHolster.transform) {
                        //print("left found right holster");
                        //check if right holster has a weapon
                        if (weaponInRightHolster) {
                            //print("right holster not empty");
                            continue; //breaks out and continues loop. should drop if didnt find another holster
                        }
                        //print("right holster IS empty, holster weapon");

                        //holster should be empty
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
                        leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
                        leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                        leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

                        //print(gameObject.name + ": holstering " + leftWeaponGameObj.name + " in the right holster using left hand");
                        weaponInRightHolster = leftWeaponGameObj;
                        leftWeaponGameObj = null;

                        weaponInteraction.UnassignWeapon(side);
                        RpcHolster(side, false);
                        return;

                    } else if (hits[i].transform == leftHolster.transform) {
                        //print("left found left holster");
                        //check if right holster has a weapon
                        if (weaponInLeftHolster) {
                            //print("left holster not empty");
                            continue; //breaks out and continues loop. should drop if didnt find another holster
                        }

                        //print("left holster IS empty, holster weapon");

                        //holster should be empty
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
                        leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
                        leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
                        leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                        leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

                        //print(gameObject.name + ": holstering " + leftWeaponGameObj.name + " in the left holster using left hand");
                        weaponInLeftHolster = leftWeaponGameObj;
                        leftWeaponGameObj = null;

                        weaponInteraction.UnassignWeapon(side);
                        RpcHolster(side, true);
                        return;
                    }

                    //no holsters found, drop weapon
                }
            }

            if (needsToDrop && leftWeaponGameObj) {
                //print("gets to drop weapon left on server");
                //print(gameObject.name + ": dropping left hand weapon: " + leftWeaponGameObj.name);
                leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
                leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;


                leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
                leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
                leftWeaponGameObj = null;
                weaponInteraction.UnassignWeapon(side);

                RpcDropWeapon(side);
            }
        } else {
            RaycastHit[] hits = Physics.SphereCastAll(rightHand.position, radius, transform.forward);

            bool needsToDrop = false;

            if (hits.Length > 0) {
                Debug.LogWarning("right hits length of " + hits.Length);
                for (int i = 0; i < hits.Length; i++) {
                    needsToDrop = true;
                    rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

                    if (hits[i].transform == rightHolster.transform) {
                        //print("right found right holster");
                        //check if right holster has a weapon
                        if (weaponInRightHolster) {
                            //print("right holster not empty");
                            continue; //breaks out and continues loop. should drop if didnt find another holster
                        }
                        //print("right holster IS empty, holster weapon");

                        //holster should be empty
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
                        rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
                        rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                        rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;
                        
                        //print(gameObject.name + ": holstering " + rightWeaponGameObj.name + " in the right holster using right hand");
                        weaponInRightHolster = rightWeaponGameObj;
                        rightWeaponGameObj = null;

                        weaponInteraction.UnassignWeapon(side);
                        RpcHolster(side, false);
                        return;

                    } else if (hits[i].transform == leftHolster.transform) {
                        //print("right found left holster");
                        //check if right holster has a weapon
                        if (weaponInLeftHolster) {
                            //print("left holster not empty");
                            continue; //breaks out and continues loop. should drop if didnt find another holster
                        }

                        //print("left holster IS empty, holster weapon");

                        //holster should be empty
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
                        rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
                        rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
                        rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                        rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;
                        
                        //print(gameObject.name + ": holstering " + rightWeaponGameObj.name + " in the left holster using left hand");
                        weaponInLeftHolster = rightWeaponGameObj;
                        rightWeaponGameObj = null;

                        weaponInteraction.UnassignWeapon(side);
                        RpcHolster(side, true);
                        return;
                    }
                }
            }

            if (needsToDrop && rightWeaponGameObj) {
                //print(gameObject.name + ": dropping right hand weapon: " + rightWeaponGameObj.name);
                rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
                rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
                rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

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