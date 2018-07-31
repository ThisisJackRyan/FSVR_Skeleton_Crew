using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Networking;

public class PathFollower : MonoBehaviour {

	public NodePath path;
	int currentNode, nextNode;
	public float speed = 1;

	[SerializeField]
	float timeToNextNode = 1f;
	float currentLerpTime;

	Vector3 startPos;
	Vector3 endPos;

	bool canMove = false;

	protected void Start() {
		currentNode = 0;
		nextNode = 1;
		currRot = transform.rotation;
		nextRot = CalcRotation( path.Nodes[nextNode] );

	}


	public void StartMoving() {
		canMove = true;
	}

	protected void Update() {
		//if (Input.GetKeyDown(KeyCode.Space)) {
		//	StartMoving();
		//}


		if (!canMove) {
			return;
		}

		if (nextNode < path.Nodes.Length) {
			MovePosition();
		} else {
			print("next node too high " + nextNode + " " + path.Nodes.Length);
		}
	}

#pragma warning disable 0219

	void MovePosition() {
		//increment timer once per frame
		currentLerpTime += Time.deltaTime * speed;
		if (currentLerpTime > timeToNextNode) {
			currentLerpTime = timeToNextNode;
		}

		//lerp!
		float perc = currentLerpTime / timeToNextNode;
		transform.position = Vector3.Lerp(path.Nodes[currentNode].position, path.Nodes[nextNode].position, perc);

		//lerp rotation
		//print("curr " + currRot + " next " + nextRot);
		transform.rotation = Quaternion.Lerp( currRot, nextRot, perc );

		if (perc >= 1) {
			IncrementNode();
		}
	}

	Quaternion currRot, nextRot;

	Quaternion CalcRotation(Transform target) {
		Vector3 vectorToTarget = target.transform.position - transform.position;
		Vector3 facingDirection = transform.forward; // just for clarity!

		float angleInDegrees = Vector3.Angle( facingDirection, vectorToTarget );
		Quaternion rotation = Quaternion.FromToRotation( facingDirection, vectorToTarget );

		return rotation * transform.rotation;
	}



	void IncrementNode() {
		currentNode = nextNode;
		if (this.nextNode < path.Nodes.Length - 1) {
			nextNode++;
		}

		currentLerpTime = 0f;

		path.Nodes[currentNode].GetComponent<EncounterNode>().CmdSpawn();

		//update rot values
		currRot = nextRot;
		nextRot = CalcRotation(path.Nodes[nextNode]);
	}

}