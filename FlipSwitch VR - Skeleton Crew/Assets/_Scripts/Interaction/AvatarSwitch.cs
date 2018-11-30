using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable 0414
public class AvatarSwitch : MonoBehaviour, IInteractible {

	public enum ChangeType {
		Color, Armor
	}

	public ChangeType type;
	public int armorSet, skinColor;
	float timer = 0;
	bool active = false;
    GameObject activator;


    public void OnActivate() {
		//print("on activate was called");
		if ( type == ChangeType.Armor ) {
			//print( "on activate was armor" );
			activator.GetComponentInParent<ChangeAvatar>().SetArmorSet( armorSet );
		} else if (type == ChangeType.Color) {
			//print( "on activate was color" );
			activator.GetComponentInParent<ChangeAvatar>().SetSkin( skinColor );
		}
	}

	//private void OnTriggerEnter( Collider other ) {
	//	//print("trigger entered");
	//	timer = 0;
	//	active = true;
	//}

	//private void OnTriggerStay( Collider other ) {
	//	if ( other.gameObject.GetComponentInParent<ChangeAvatar>() && active ) {
 //           activator = other.gameObject.GetComponentInParent<ChangeAvatar>().gameObject;

 //           timer += Time.deltaTime;

	//		if ( timer >= 0.5f ) {
	//			OnActivate();
	//			active = false;
	//		}
	//	}
	//}

	//private void OnTriggerExit( Collider other ) {
 //       active = false;
	//}

	public bool Interact(GameObject interactingObject, bool isLeft) {
		activator = interactingObject.GetComponentInParent<ChangeAvatar>().gameObject;
		OnActivate();
		return true;
	}
}
