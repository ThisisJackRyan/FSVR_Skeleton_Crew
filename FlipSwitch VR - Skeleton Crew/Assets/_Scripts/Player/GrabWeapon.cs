﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent( typeof( WeaponInteraction ) )]
public class GrabWeapon : NetworkBehaviour {
	private WeaponInteraction weaponInteraction;
	public float radius = 0.1f;

	public Transform rightHand, leftHand;
	public GameObject rightHolster, leftHolster;
	public AudioClip holsterClip, drawClip;
	private GameObject weaponInRightHolster, weaponInLeftHolster;

	private GameObject leftWeaponGameObj;
	private GameObject rightWeaponGameObj;
	private GameObject leftHighlightedWeaponObj;
	private GameObject rightHighlightedWeaponObj;
    private GameObject leftHighlightedHolsterObj;
    private GameObject rightHighlightedHolsterObj;
    private bool isDead;

	public Transform leftHandIKSolver;
	public Quaternion leftHandOffSetWhenHolding;
	public Quaternion leftHandOriginalRot;

	// Use this for initialization
	void Start() {
		weaponInteraction = GetComponent<WeaponInteraction>();
		leftHandOriginalRot = leftHandIKSolver.rotation;
	}

	public float cooldown = 0.5f;
	bool canGrabRight = true, canGrabLeft = true;

	IEnumerator GrabCooldownRight() {
		canGrabRight = false;
		yield return new WaitForSecondsRealtime( cooldown );
		canGrabRight = true;
	}

	IEnumerator GrabCooldownLeft() {
		canGrabLeft = false;
		yield return new WaitForSecondsRealtime( cooldown );
		canGrabLeft = true;
	}

	public void Death() {
		if ( isLocalPlayer ) {
			if ( rightWeaponGameObj ) {
				CmdDropIfHolding( "right", gameObject );
			}
			if ( leftWeaponGameObj ) {
				CmdDropIfHolding( "left", gameObject );
			}
		}

		if ( weaponInRightHolster ) {
			////print( "weapon in right holster: " + weaponInRightHolster.name );
			weaponInRightHolster.GetComponent<ObjectPositionLock>().posPoint = null;
			weaponInRightHolster.GetComponent<ObjectPositionLock>().posOffset = Vector3.zero;
			weaponInRightHolster.GetComponent<ObjectPositionLock>().rotOffset = Quaternion.Euler( Vector3.zero );
            weaponInRightHolster.GetComponent<Weapon>().playerWhoHolstered = null;


            //if (isServer) {
				weaponInRightHolster.GetComponent<Rigidbody>().isKinematic = false;
			//}
		}
		if ( weaponInLeftHolster ) {
			weaponInLeftHolster.GetComponent<ObjectPositionLock>().posPoint = null;
			weaponInLeftHolster.GetComponent<ObjectPositionLock>().posOffset = Vector3.zero;
			weaponInLeftHolster.GetComponent<ObjectPositionLock>().rotOffset = Quaternion.Euler( Vector3.zero );
            weaponInLeftHolster.GetComponent<Weapon>().playerWhoHolstered = null;

            //if (isServer) {
				weaponInLeftHolster.GetComponent<Rigidbody>().isKinematic = false;
			//}
		}

		if (isServer) {
			if ( leftHighlightedWeaponObj ) {
				SendCommandToUnHighlight(true);
			}
			if ( rightHighlightedWeaponObj ) {
				SendCommandToUnHighlight(false);
			}
		}

		isDead = true;
	}

	public void Revive() {
		isDead = false;
	}

	void Update() {
		if ( !isLocalPlayer || isDead ) {
			return;
		}

		if ( rightWeaponGameObj ) {
			if ( Controller.RightController.GetPressDown( Controller.Grip ) ) {
				CmdDropIfHolding( "right", gameObject );
				//run highlight logic here

			}
		} else {
			if ( canGrabRight ) {
				if ( Controller.RightController.GetPressDown( Controller.Grip ) ) {
					CmdGrabIfHighlighted( "right" );
					//run highlight logic here
				}
			}
		}

		if ( leftWeaponGameObj ) {
			if ( Controller.LeftController.GetPressDown( Controller.Grip ) ) {
				CmdDropIfHolding( "left", gameObject );
				//run highlight logic here
				StopCoroutine( "GrabCooldownLeft" );
				StartCoroutine( "GrabCooldownLeft" );
			}
		} else {
			if ( canGrabLeft ) {
				if ( Controller.LeftController.GetPressDown( Controller.Grip ) ) {
					CmdGrabIfHighlighted( "left" );
					//run highlight logic here
				}
			}
		}

	}

	public void SendCommandToHighlight( bool isLeft ) {
        //if (isServer) {
        //	return;
        //}

        ////print(name + " highlight command received");


        if ( isLeft ) {
            ////print(name + " is left");

            if ( !leftWeaponGameObj && canGrabLeft ) {
                ////print(name + " highlight left weapon");

                FindAndHighlightNearestWeapon( "left", gameObject );
            } else if(leftWeaponGameObj) {
                ////print(name + " highlight holster for left");

                FindAndHighlightNearestHolster(isLeft, gameObject);
            }
		} else {
            ////print(name + " is right");

            if ( !rightWeaponGameObj && canGrabRight ) {
                ////print(name + " highlight right weapon");

                FindAndHighlightNearestWeapon( "right", gameObject );
            } else if (rightWeaponGameObj) {
                ////print(name + " highlight holster for right");

                FindAndHighlightNearestHolster(isLeft, gameObject);
            }
        }
	}

	public void SendCommandToUnHighlight( bool isLeft ) {
		//print("called unhighlight cmd");
		if (!isServer) {
			return;
		}

		if ( isLeft ) {
			leftHighlightedWeaponObj = null;
			RpcUnhighlightWeapon( "left", gameObject );
            leftHighlightedHolsterObj = null;
            RpcUnhighlightHolster(true, gameObject);

        } else {
			rightHighlightedWeaponObj = null;
			RpcUnhighlightWeapon( "right", gameObject );
            rightHighlightedHolsterObj = null;
            RpcUnhighlightHolster(false, gameObject);

        }
    }

	private void FindAndHighlightNearestWeapon( string side, GameObject player ) {
		Transform hand = side.Equals( "left" ) ? leftHand : rightHand;
        
		Collider[] hits = Physics.OverlapSphere( hand.position, radius );
		GameObject closest = null;
		bool hitWeapon = false;

		if ( hits.Length > 0 ) {
            //////////print("hits > 0 with " + hits.Length);
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].transform.tag == "WeaponPickup") {
                    if (hits[i].transform.GetComponentInParent<Weapon>().isBeingHeldByPlayer) {
                        continue;
                    }

                    if (hits[i].transform.GetComponentInParent<Weapon>().playerWhoHolstered != player && hits[i].transform.GetComponentInParent<Weapon>().playerWhoHolstered != null) {
                        continue;
                    }

                    hitWeapon = true;
                    if (!closest) {
                        closest = hits[i].transform.gameObject;
                    } else {
                        if (Mathf.Abs(Vector3.Distance(hand.position, hits[i].transform.position)) <
                            Mathf.Abs(Vector3.Distance(hand.position, closest.transform.position))) {
                            //////////print("wjbwefkjgb");
                            closest = hits[i].transform.gameObject;
                        }
                    }
                }
            }
        }

		if ( closest ) {
			if ( Mathf.Abs( Vector3.Distance( closest.transform.position, hand.position ) ) >= radius || hitWeapon == false ) {
				////////print("no weapon in range");
				closest = null;

				if ( side.Equals( "left" ) ) {
					leftHighlightedWeaponObj = null;
				} else if ( side.Equals( "right" ) ) {
					rightHighlightedWeaponObj = null;
				}

				RpcUnhighlightWeapon( side, player );
			} else {
				if ( side.Equals( "left" ) ) {
					leftHighlightedWeaponObj = closest.transform.root.gameObject;
				} else if ( side.Equals( "right" ) ) {
					rightHighlightedWeaponObj = closest.transform.root.gameObject;
				}

				RpcHighlightWeapon( side, closest.transform.root.gameObject, player );
			}
		}
	}

	#region Commands

	[Command]
	private void CmdGrabIfHighlighted( string side ) {
		Transform hand = side.Equals( "left" ) ? leftHand : rightHand;
		GameObject temp = null;

		if ( side.Equals( "left" ) ) {
			////////print("left hi " + leftHighlightedWeaponObj);
			if ( leftHighlightedWeaponObj ) {
				temp = leftWeaponGameObj = leftHighlightedWeaponObj;
				leftHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
				leftHighlightedWeaponObj = null;
			}
		} else {
			////////print("right hi " + rightHighlightedWeaponObj);

			if ( rightHighlightedWeaponObj ) {
				temp = rightWeaponGameObj = rightHighlightedWeaponObj;
				rightHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
				rightHighlightedWeaponObj = null;
			}
		}

		if ( temp != null ) { //the actual equipping of the weapon
			temp.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
			if (side.Equals("left")) {
				temp.GetComponent<ObjectPositionLock>().posOffset = temp.GetComponent<Weapon>().data.heldPositionLeft;
				temp.GetComponent<ObjectPositionLock>().rotOffset = temp.GetComponent<Weapon>().data.heldRotationLeft;

			} else {
				temp.GetComponent<ObjectPositionLock>().posOffset = temp.GetComponent<Weapon>().data.heldPositionRight;
				temp.GetComponent<ObjectPositionLock>().rotOffset = temp.GetComponent<Weapon>().data.heldRotationRight;

			}
			temp.GetComponent<Weapon>().isBeingHeldByPlayer = true;
			temp.GetComponent<Weapon>().playerWhoHolstered = null;

			temp.GetComponent<Rigidbody>().isKinematic = true;

			if ( temp == weaponInRightHolster ) {
				////////print("found weapon in right holster");
				weaponInRightHolster = null;
				//RpcPlayDrawSound();
			} else if ( temp == weaponInLeftHolster ) {
				////////print("found weapon in left holster");

				weaponInLeftHolster = null;
				//RpcPlayDrawSound();
			}

			weaponInteraction.AssignWeapon( side, temp );

			RpcChangeanimatorState(side + temp.GetComponent<Weapon>().data.gripType.ToString(), true);
			
			RpcGrabWeapon( side, temp );
			temp.GetComponent<Weapon>().TurnOnFire();
		}
	}

	[ClientRpc]
	void RpcChangeanimatorState(string state, bool value) {
		GetComponent<NetworkAnimator>().animator.SetBool(state, value);

		//if (state.Contains("left")) {
		//	if(value){
		//		//grabbibg weapon
		//		leftHandIKSolver.SetPositionAndRotation(leftHandIKSolver.position,  leftHandOffSetWhenHolding);
		//	} else {
		//		//dropping
		//		leftHandIKSolver.SetPositionAndRotation(leftHandIKSolver.position, leftHandOriginalRot);

		//	}
		//}

	}

	void PlayDrawSound() {
		GetComponent<AudioSource>().PlayOneShot( drawClip );
	}

	[Command]
	private void CmdDropIfHolding( string side, GameObject player ) {
		if ( side.Equals( "right" ) ) {
			////////print("calls handle dropping right in cmd " + rightWeaponGameObj);

			if ( rightWeaponGameObj != null ) {
				////////print("right weapon exist, drop it");
				HandleDropping( "right", player );
			}
		} else {
			////////print("calls handle dropping left in cmd " + leftWeaponGameObj);

			if ( leftWeaponGameObj != null ) {
				////////print("left weapon exist, drop it");

				HandleDropping( "left", player );
			}
		}
	}

	#endregion

	#region RPC

	[ClientRpc]
	private void RpcUnhighlightWeapon( string side, GameObject player ) {
		//print("called unhighlight rpc " + side + " " + player.name + " " + isServer);

		if ( isServer) {
			return;
		}

		if ( isLocalPlayer && player == gameObject ) {
			if ( side.Equals( "left" ) ) {
				if ( leftHighlightedWeaponObj ) {
					leftHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
					leftHighlightedWeaponObj = null;
				}
			} else {
				if ( rightHighlightedWeaponObj ) {
					rightHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
					rightHighlightedWeaponObj = null;
				}
			}
		}
	}

	[ClientRpc]
	private void RpcHighlightWeapon( string side, GameObject weaponToHighlight, GameObject player ) {
		if ( isServer ) {
			return;
		}

		if ( isLocalPlayer && player == gameObject ) {
			if ( side.Equals( "left" ) ) {
				if ( leftHighlightedWeaponObj ) {
					leftHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
				}

				weaponToHighlight.GetComponent<Weapon>().myOutline.OutlineColor = Color.blue;
				weaponToHighlight.GetComponent<Weapon>().myOutline.enabled = true;
				leftHighlightedWeaponObj = weaponToHighlight;
			} else {
				if ( rightHighlightedWeaponObj ) {
					rightHighlightedWeaponObj.GetComponent<Weapon>().myOutline.enabled = false;
				}

				weaponToHighlight.GetComponent<Weapon>().myOutline.OutlineColor = Color.red;
				weaponToHighlight.GetComponent<Weapon>().myOutline.enabled = true;
				rightHighlightedWeaponObj = weaponToHighlight;
			}
		} else if ( player == gameObject ) {
			if ( side.Equals( "left" ) ) {
				leftHighlightedWeaponObj = weaponToHighlight;
			} else {
				rightHighlightedWeaponObj = weaponToHighlight;
			}
		}
	}

	[ClientRpc]
	private void RpcGrabWeapon( string side, GameObject weapon ) {
		if ( isServer ) {
			return;
		}

		Transform hand;
		if ( side.Equals( "left" ) ) {
			leftWeaponGameObj = weapon;
			hand = leftHand;
			//////////print(gameObject.name + ": setting left hand weapon being held to " + weapon.name);

			leftHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
			leftHighlightedWeaponObj = null;
		} else {
			rightWeaponGameObj = weapon;
			hand = rightHand;
			//////////print(gameObject.name + ": setting right hand weapon being held to " + weapon.name);

			rightHighlightedWeaponObj.GetComponent<Outline>().enabled = false;
			rightHighlightedWeaponObj = null;
		}

		weapon.GetComponent<ObjectPositionLock>().posPoint = hand.gameObject;
		if (side.Equals("left")) {
			weapon.GetComponent<ObjectPositionLock>().posOffset = weapon.GetComponent<Weapon>().data.heldPositionLeft;
			weapon.GetComponent<ObjectPositionLock>().rotOffset = weapon.GetComponent<Weapon>().data.heldRotationLeft;
		} else {
			weapon.GetComponent<ObjectPositionLock>().posOffset = weapon.GetComponent<Weapon>().data.heldPositionRight;
			weapon.GetComponent<ObjectPositionLock>().rotOffset = weapon.GetComponent<Weapon>().data.heldRotationRight;
		}
		weapon.GetComponent<Weapon>().isBeingHeldByPlayer = true;
		weapon.GetComponent<Weapon>().playerWhoHolstered = null;


		weapon.GetComponent<Rigidbody>().isKinematic = true;

		weaponInteraction.AssignWeapon( side, weapon );
		weapon.GetComponent<Weapon>().TurnOnFire();

		PlayDrawSound();
	}

	[ClientRpc]
	private void RpcDropWeapon( string side ) {
		if ( isServer )
			return;

		////////print("rpc drop weapon called on client");

		if ( side.Equals( "left" ) ) {
			leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
			leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

			leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
            leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
            leftWeaponGameObj = null;
			weaponInteraction.UnassignWeapon( side );

			if ( isLocalPlayer ) {
				StopCoroutine( "GrabCooldownLeft" );
				StartCoroutine( "GrabCooldownLeft" );
			}
		} else {
			rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
			rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

			rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
            rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
            rightWeaponGameObj = null;
			weaponInteraction.UnassignWeapon( side );

			if ( isLocalPlayer ) {
				StopCoroutine( "GrabCooldownRight" );
				StartCoroutine( "GrabCooldownRight" );
			}
		}


	}

	[ClientRpc]
	private void RpcHolster( string side, bool holsterLeft ) {
		if ( isServer )
			return;

		////////print("rpc holster called on client");

		GameObject holster = ( holsterLeft ) ? leftHolster : rightHolster;

		if ( side.Equals( "left" ) ) {
			////////print("rpc side is left and the weapon is " + leftWeaponGameObj);
			leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster;
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
			leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
			leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;

			if ( holster == rightHolster ) {
				////////print("rpc side is left with right holster");
				weaponInRightHolster = leftWeaponGameObj;
			} else if ( holster == leftHolster ) {
				////////print("rpc side is left with left holster");
				weaponInLeftHolster = leftWeaponGameObj;
			}

			leftWeaponGameObj = null;

			GetComponent<AudioSource>().PlayOneShot(holsterClip);

			if ( isLocalPlayer ) {
				StopCoroutine( "GrabCooldownLeft" );
				StartCoroutine( "GrabCooldownLeft" );
			}
		} else {
			////////print("rpc side is right and the weapon is " + rightWeaponGameObj);

			rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = holster;
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
			rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
			rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;

			////////print("holster is " + holster.name);
			////////print("right holster is " + rightHolster.name);

			if ( holster == rightHolster ) {
				////////print("rpc side is right with right holster");
				weaponInRightHolster = rightWeaponGameObj;
			} else if ( holster == leftHolster ) {
				////////print("rpc side is right with left holster");
				weaponInLeftHolster = rightWeaponGameObj;
			}

			rightWeaponGameObj = null;
			GetComponent<AudioSource>().PlayOneShot( holsterClip );

			if ( isLocalPlayer ) {
				StopCoroutine( "GrabCooldownRight" );
				StartCoroutine( "GrabCooldownRight" );
			}
		}

		weaponInteraction.UnassignWeapon( side );
	}

    #endregion

    #region holster highlighting
    private void FindAndHighlightNearestHolster(bool isLeft, GameObject player) {
        Transform hand = isLeft ? leftHand : rightHand;

        Collider[] hits = Physics.OverlapSphere(hand.position, radius);
        GameObject closest = null;
        bool hitHolster = false;
        bool holsterIsLeft = false;

        if (hits.Length > 0) {
            ////print("hits > 0 with " + hits.Length);
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].transform == leftHolster.transform) {
                    if (weaponInLeftHolster) {
                        ////print(name + "weapon in left holster");

                        continue;
                    }

                    hitHolster = true;
                    holsterIsLeft = true;
                    if (!closest) {
                        closest = hits[i].transform.gameObject;
                    } else {
                        if (Mathf.Abs(Vector3.Distance(hand.position, hits[i].transform.position)) <
                            Mathf.Abs(Vector3.Distance(hand.position, closest.transform.position))) {
                            ////print("changing closest");
                            closest = hits[i].transform.gameObject;
                        }
                    }
                } else if (hits[i].transform == rightHolster.transform) {
                    if (weaponInRightHolster) {
                        ////print(name + "weapon in right holster");

                        continue;
                    }

                    hitHolster = true;
                    holsterIsLeft = false;
                    if (!closest) {
                        closest = hits[i].transform.gameObject;
                    } else {
                        if (Mathf.Abs(Vector3.Distance(hand.position, hits[i].transform.position)) <
                            Mathf.Abs(Vector3.Distance(hand.position, closest.transform.position))) {
                            ////print("changing closest");

                            closest = hits[i].transform.gameObject;
                        }
                    }
                }
            }
        }

        if (closest) {
            if (Mathf.Abs(Vector3.Distance(closest.transform.position, hand.position)) >= radius || hitHolster == false) {
                ////print("no holster in range");
                closest = null;

                if (isLeft) {
                    leftHighlightedHolsterObj = null;
                } else  {
                    rightHighlightedHolsterObj = null;
                }

                RpcUnhighlightHolster(isLeft, player);
            } else {
                GameObject holsterGo = (holsterIsLeft) ? leftHolster : rightHolster;

                if (isLeft) {
                    ////print("is left, setting left highlighted obj to " + leftHighlightedHolsterObj);

                    leftHighlightedHolsterObj = holsterGo;
                } else {
                    ////print("is right, setting right highlighted obj to " + rightHighlightedWeaponObj);

                    rightHighlightedHolsterObj = holsterGo;
                }

                RpcHighlightHolster(isLeft,holsterIsLeft, player);
            }
        }
    }

    [ClientRpc]
    private void RpcUnhighlightHolster(bool isLeft, GameObject player) {
		//print("called unhighlight rpc holster");

		if (isServer)
            return;

        if (isLocalPlayer && player == gameObject) {
            if (isLeft) {
                if (leftHighlightedHolsterObj) {
                    leftHighlightedHolsterObj.GetComponent<Outline>().enabled = false;
                    leftHighlightedHolsterObj = null;
                }
            } else {
                if (rightHighlightedHolsterObj) {
                    rightHighlightedHolsterObj.GetComponent<Outline>().enabled = false;
                    rightHighlightedHolsterObj = null;
                }
            }
        }
    }

    [ClientRpc]
    private void RpcHighlightHolster(bool isLeft, bool holsterIsLeft, GameObject player) {
        if (isServer) {
            return;
        }

        GameObject holsterGo = (holsterIsLeft) ? leftHolster: rightHolster;
        if (isLocalPlayer && player == gameObject) {

            if (isLeft) {
                if (leftHighlightedHolsterObj) {
                    leftHighlightedHolsterObj.GetComponent<Outline>().enabled = false;
                }
              
                holsterGo.GetComponent<Outline>().OutlineColor = Color.blue;
                holsterGo.GetComponent<Outline>().enabled = true;
                leftHighlightedHolsterObj = holsterGo;
            } else {
                if (rightHighlightedHolsterObj) {
                    rightHighlightedHolsterObj.GetComponent<Outline>().enabled = false;
                }

                holsterGo.GetComponent<Outline>().OutlineColor = Color.red;
                holsterGo.GetComponent<Outline>().enabled = true;
                rightHighlightedHolsterObj = holsterGo;
            }
        } else if (player == gameObject) {
            if (isLeft) {
                leftHighlightedHolsterObj = holsterGo;
            } else {
                rightHighlightedHolsterObj = holsterGo;
            }
        }
    }
    #endregion

    void HandleDropping( string side, GameObject player ) {
		//Debug.LogWarning("drop called for " + side);

		if ( side.Equals( "left" ) ) {
			Collider[] hits = Physics.OverlapSphere( leftHand.position, radius );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {
				//Debug.LogWarning("left hits length of " + hits.Length);

				for ( int i = 0; i < hits.Length; i++ ) {
					needsToDrop = true;
					leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

					//handle anim change regardless of holster or drop
					//GetComponent<NetworkAnimator>().animator.SetBool();
					RpcChangeanimatorState(side + leftWeaponGameObj.GetComponent<Weapon>().data.gripType.ToString(), false);


					if ( hits[i].transform == rightHolster.transform ) {
						////////print("left found right holster");
						//check if right holster has a weapon
						if ( weaponInRightHolster ) {
							////////print("right holster not empty");
							continue; //breaks out and continues loop. should drop if didnt find another holster
						}
						////////print("right holster IS empty, holster weapon");

						//holster should be empty
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
						leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
						leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

						////////print(gameObject.name + ": holstering " + leftWeaponGameObj.name + " in the right holster using left hand");
						weaponInRightHolster = leftWeaponGameObj;
						leftWeaponGameObj = null;

						weaponInteraction.UnassignWeapon( side );
						RpcHolster( side, false );
						return;

					} else if ( hits[i].transform == leftHolster.transform ) {
						////////print("left found left holster");
						//check if right holster has a weapon
						if ( weaponInLeftHolster ) {
							////////print("left holster not empty");
							continue; //breaks out and continues loop. should drop if didnt find another holster
						}

						////////print("left holster IS empty, holster weapon");

						//holster should be empty
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						leftWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = leftWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
						leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
						leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

						////////print(gameObject.name + ": holstering " + leftWeaponGameObj.name + " in the left holster using left hand");
						weaponInLeftHolster = leftWeaponGameObj;
						leftWeaponGameObj = null;

						weaponInteraction.UnassignWeapon( side );
						RpcHolster( side, true );
						return;
					}

					//no holsters found, drop weapon
				}
			}

			if ( needsToDrop && leftWeaponGameObj ) {
				////////print("gets to drop weapon left on server");
				////////print(gameObject.name + ": dropping left hand weapon: " + leftWeaponGameObj.name);
				leftWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
				leftWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
				leftWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;


				leftWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
				leftWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;
				leftWeaponGameObj = null;
				weaponInteraction.UnassignWeapon( side );

				RpcDropWeapon( side );
			}
		} else {
			Collider[] hits = Physics.OverlapSphere( rightHand.position, radius );

			bool needsToDrop = false;

			if ( hits.Length > 0 ) {
				//Debug.LogWarning("right hits length of " + hits.Length);
				for ( int i = 0; i < hits.Length; i++ ) {
					needsToDrop = true;
					rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();

					//handle anim change regardless of drop or holster
					//GetComponent<NetworkAnimator>().animator.SetBool();
					RpcChangeanimatorState(side + rightWeaponGameObj.GetComponent<Weapon>().data.gripType.ToString(), false);

					if ( hits[i].transform == rightHolster.transform ) {
						////////print("right found right holster");
						//check if right holster has a weapon
						if ( weaponInRightHolster ) {
							////////print("right holster not empty");
                            //RpcUnhighlightHolster(false, player);
                            ///
							continue; //breaks out and continues loop. should drop if didnt find another holster
						}
						////////print("right holster IS empty, holster weapon");

						//holster should be empty
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
						rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
						rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

						////////print(gameObject.name + ": holstering " + rightWeaponGameObj.name + " in the right holster using right hand");
						weaponInRightHolster = rightWeaponGameObj;
						rightWeaponGameObj = null;

						weaponInteraction.UnassignWeapon( side );
						RpcHolster( side, false );
						return;

					} else if ( hits[i].transform == leftHolster.transform ) {
						////////print("right found left holster");
						//check if right holster has a weapon
						if ( weaponInLeftHolster ) {
							////////print("left holster not empty");
                            //RpcUnhighlightHolster(true, player);
                            ///
							continue; //breaks out and continues loop. should drop if didnt find another holster
						}

						////////print("left holster IS empty, holster weapon");

						//holster should be empty
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = hits[i].transform.gameObject;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().posOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredPosition;
						rightWeaponGameObj.GetComponent<ObjectPositionLock>().rotOffset = rightWeaponGameObj.GetComponent<Weapon>().data.holsteredRotation;
						rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = true;
						rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
						rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = player;

						////////print(gameObject.name + ": holstering " + rightWeaponGameObj.name + " in the left holster using left hand");
						weaponInLeftHolster = rightWeaponGameObj;
						rightWeaponGameObj = null;

						weaponInteraction.UnassignWeapon( side );
						RpcHolster( side, true );
						return;
					}
				}
			}

			if ( needsToDrop && rightWeaponGameObj ) {
				////////print(gameObject.name + ": dropping right hand weapon: " + rightWeaponGameObj.name);
				rightWeaponGameObj.GetComponent<Weapon>().TurnOffFire();
				rightWeaponGameObj.GetComponent<Weapon>().isBeingHeldByPlayer = false;
				rightWeaponGameObj.GetComponent<Weapon>().playerWhoHolstered = null;

				rightWeaponGameObj.GetComponent<ObjectPositionLock>().posPoint = null;
				rightWeaponGameObj.GetComponent<Rigidbody>().isKinematic = false;

				rightWeaponGameObj = null;
				weaponInteraction.UnassignWeapon( side );

				RpcDropWeapon( side );
			}
		}
	}

	private void OnDrawGizmosSelected() {
		Gizmos.DrawSphere( rightHand.transform.position, radius );
		Gizmos.DrawSphere( leftHand.transform.position, radius );
	}
}