using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeaponInteraction : NetworkBehaviour {

	public GameObject rightHandWeapon;
	public Weapon rightWeaponScript;
	public bool rightHandIsInteractable;

	public GameObject leftHandWeapon;
	public Weapon leftWeaponScript;
	public bool leftHandIsInteractable;

	public MastInteraction mastInteraction;
    public CannonInteraction cannonInteraction;

	public void AssignWeapon(string side, GameObject weapon ) {
		if ( side.Equals( "left" ) ) {
			leftHandWeapon = weapon;
			leftWeaponScript = weapon.GetComponent<Weapon>();
			mastInteraction.emptyLeftHand = false;
			leftHandIsInteractable = ( leftWeaponScript.data.type == WeaponData.WeaponType.Melee ) ? false : true;
			if ( leftWeaponScript.data.type == WeaponData.WeaponType.Punt )
				leftWeaponScript.owningPlayerCannonScript = cannonInteraction;
		} else {
			rightHandWeapon = weapon;
			rightWeaponScript = weapon.GetComponent<Weapon>();
			mastInteraction.emptyRightHand = false;
			rightHandIsInteractable = ( rightWeaponScript.data.type == WeaponData.WeaponType.Melee ) ? false : true;
			if ( rightWeaponScript.data.type == WeaponData.WeaponType.Punt )
				rightWeaponScript.owningPlayerCannonScript = cannonInteraction;
		}
	}

	public void UnassignWeapon(string side) {
		if ( side.Equals( "left" ) ) {
			if ( leftWeaponScript.data.type == WeaponData.WeaponType.Punt )
				leftWeaponScript.owningPlayerCannonScript = null;
			leftWeaponScript = null;
			leftHandWeapon = null;
			leftHandIsInteractable = false;
			mastInteraction.emptyLeftHand = true;
		} else {
			if ( rightWeaponScript.data.type == WeaponData.WeaponType.Punt )
				rightWeaponScript.owningPlayerCannonScript = null;
			rightHandIsInteractable = false;
			rightWeaponScript = null;
			rightHandWeapon = null;
			mastInteraction.emptyRightHand = true;
		}
	}

	private void Start() {
		mastInteraction = GetComponent<MastInteraction>();
        cannonInteraction = GetComponent<CannonInteraction>();
    }

  

	private void Update() {
		if ( !isLocalPlayer )
			return;

		if(leftHandIsInteractable && Controller.LeftController.GetPressDown( Controller.Trigger ) ) {
			if ( leftWeaponScript.data.type == WeaponData.WeaponType.Punt ) {
				CmdToggleFire("left");
			} else if ( leftWeaponScript.data.type == WeaponData.WeaponType.Gun ) {
				CmdFireWeapon( "left" );
			}
		}

		if ( rightHandIsInteractable && Controller.RightController.GetPressDown( Controller.Trigger ) ) {
			if ( rightWeaponScript.data.type == WeaponData.WeaponType.Punt ) {
				CmdToggleFire( "right" );
			} else if ( rightWeaponScript.data.type == WeaponData.WeaponType.Gun ) {
				CmdFireWeapon( "right" );
			}
		}
	}

	[Command]
	private void CmdFireWeapon(string side ) {
		if ( side.Equals( "left" ) )
			leftWeaponScript.SpawnBullet();
		else
			rightWeaponScript.SpawnBullet();
		RpcFireWeapon( side );
	}

	[ClientRpc]
	private void RpcFireWeapon(string side ) {
		if (isServer)
			return;
		if ( side.Equals( "left" ) )
			leftWeaponScript.SpawnBullet();
		else
			rightWeaponScript.SpawnBullet();
	}

	[Command]
	private void CmdToggleFire( string side) {
		if ( side.Equals( "left" ) )
			leftWeaponScript.ToggleFire();
		else
			rightWeaponScript.ToggleFire();
		RpcToggleFire( side);
	}

	[ClientRpc]
	private void RpcToggleFire( string side) {
		if (isServer)
			return;
		if ( side.Equals( "left" ) )
			leftWeaponScript.ToggleFire();
		else
			rightWeaponScript.ToggleFire();
	}
}
