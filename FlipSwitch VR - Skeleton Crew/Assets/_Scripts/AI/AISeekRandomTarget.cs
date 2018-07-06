using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AISeekRandomTarget : MonoBehaviour {

	NavMeshAgent agent;
	float walkRadius = 5;
	Vector3 goal;

	// Use this for initialization
	void Start() {
		agent = GetComponent<NavMeshAgent>();
		goal = GetRandomPosition();
		agent.destination = goal;
	}

	// Update is called once per frame
	void Update() {
		float dist = agent.remainingDistance;
		if (dist != Mathf.Infinity && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance == 0) {
			goal = GetRandomPosition();
			agent.destination = goal;
		}
	}

	Vector3 GetRandomPosition() {
		print("sfd");
		Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
		randomDirection += transform.position;
		NavMeshHit hit;
		NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);
		return hit.position;
	}
}