using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CameraPathFollower : NetworkBehaviour {

	public NodePath path;
	int currentNode, nextNode;
	public float speed = 1;
	public float maxSpeed = 2, minSpeed = 0.5f;
	[SerializeField]
	float timeToNextNode = 1f;
	float currentLerpTime;

	Vector3 startPos;
	Vector3 endPos;
	Quaternion currRot, nextRot;
	public bool canMove = false;
	public bool ignoreServer = true;
	public bool lockAxisY = true;


	protected void Update() {
		//if (Input.GetKeyDown(KeyCode.Space)) {
		//	StartMoving();
		//}

		if (!ignoreServer) {
			if ( !isServer ) {
				return;
			}
		}

		if ( !canMove ) {
			return;
		}

		if ( nextNode < path.Nodes.Length ) {
			MovePosition();
		} else {
			//print("next node too high " + nextNode + " " + path.Nodes.Length);
		}
	}

	void MovePosition() {
		//increment timer once per frame
		currentLerpTime += Time.deltaTime * speed;
		if ( currentLerpTime > timeToNextNode ) {
			currentLerpTime = timeToNextNode;
		}

		//lerp!
		float perc = currentLerpTime / timeToNextNode;
		transform.position = Vector3.Lerp( path.Nodes[currentNode].position, path.Nodes[nextNode].position, perc );

		//lerp rotation
		////print("curr " + currRot + " next " + nextRot);
		transform.rotation = Quaternion.Lerp( currRot, nextRot, perc );

		if ( perc >= 1 ) {
			IncrementNode();
		}
	}

	Quaternion CalcRotation( Transform target ) {
		Vector3 vectorToTarget = target.transform.position - transform.position;
		if (lockAxisY) {
			vectorToTarget.y = 0;
		}

		Vector3 facingDirection = transform.forward; // just for clarity!

		float angleInDegrees = Vector3.Angle( facingDirection, vectorToTarget );
		Quaternion rotation = Quaternion.FromToRotation( facingDirection, vectorToTarget );

		return rotation * transform.rotation;
	}

	[Button( "increment path" )]
	void IncrementNode() {
		currentNode = nextNode;
		if ( this.nextNode < path.Nodes.Length - 1 ) {
			nextNode++;
		}

		currentLerpTime = 0f;

		//update rot values
		currRot = nextRot;
		nextRot = CalcRotation( path.Nodes[nextNode].GetChild(0) );//uses node child as facing target
	}
}
