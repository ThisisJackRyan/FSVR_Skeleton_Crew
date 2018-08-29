using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class CannonAimNode : MonoBehaviour {

	public CannonInteraction player;
	public GameObject particles;
	Cannon cannon;
	bool active;
	int index;

	private void OnEnable() {
		cannon = GetComponentInParent<Cannon>();
		index = transform.GetSiblingIndex();
	}

	private void OnTriggerEnter( Collider other ) {
		if (!player || !cannon.isServer) {
			//print( "player is null, is not the server" );
			
			return;
		}
		//print( "other: " + other.transform.root );
		//print( "player: " + player.transform.root );
		if (other.transform.root == player.transform.root) {
			cannon.RotateBarrel(index);
			GetComponentInParent<Cannon>().PlayAim();
			//print("lw oiwh oiwer h o");
		}
	}

	
}
