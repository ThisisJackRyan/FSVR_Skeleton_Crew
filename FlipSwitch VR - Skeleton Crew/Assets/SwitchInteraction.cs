using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class SwitchInteraction : NetworkBehaviour {

	public float interactRadius = 0.1f;
	MastInteraction mastInteraction;

	private void Start() {
		mastInteraction = GetComponent<MastInteraction>();
	}

	void Update () {
		if (!isLocalPlayer) {
			return;
		}

		//closest hasnt been found, grabbing is allowed  //are we changing the -1 sentinel?
		if (Controller.LeftController.GetPressDown(Controller.TrackPad)) {
			print( "left track pad" );

			CmdSphereCast( true);
		}

		if (Controller.RightController.GetPressDown(Controller.TrackPad)) {
			print("right track pad");
			CmdSphereCast(true);
		}
	}

	[Command]
	void CmdSphereCast(bool isLeft) {
		if (!isServer) {
			return;
		}

		SphereCast(isLeft);
	}

	void SphereCast(bool isLeft) {
		Transform hand = (isLeft) ? mastInteraction.leftHand: mastInteraction.rightHand;
		Collider[] hits = Physics.OverlapSphere(hand.position, interactRadius);

		for (int i = 0; i < hits.Length; i++) {
			//Debug.LogWarning(hits[i].name);
			if (hits[i].transform.root == transform.root) {
				continue;
			}

			IInteractible toInteractWith = null;
			foreach (var ii in hits[i].gameObject.GetComponents<MonoBehaviour>()) {
				if (ii is IInteractible) {
					toInteractWith = (IInteractible)ii;
					break;
				} 
			}

			if (toInteractWith != null) {
				toInteractWith.Interact(transform.root.gameObject, isLeft);
			}
		}
	}

	private void OnDrawGizmos() {
		if (mastInteraction) {
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(mastInteraction.leftHand.position, interactRadius);

			Gizmos.color = Color.red;
			Gizmos.DrawSphere(mastInteraction.rightHand.position, interactRadius);
		}
	}
}

public interface IInteractible {
	 bool Interact(GameObject interactingObject, bool isLeft);
}