using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIFleeTarget : MonoBehaviour {
	NavMeshAgent agent;
	public GameObject target;
	public float fleeDistance;
	public float minDistance;
	GameObject safeTarget;

	// Use this for initialization
	void Start() {
		agent = GetComponent<NavMeshAgent>();
	}

	// Update is called once per frame
	void Update() {
		if (Vector3.Distance(transform.position, target.transform.position) < minDistance) {
			//flee
			//Vector3 noY = new Vector3(transform.position.x, 0, transform.position.z);
			//Vector3 noYTarget = new Vector3( target.transform.position.x, 0, target.transform.position.z );

			//Vector3 goal = noY- noYTarget ;
			//goal *= fleeDistance;
			if (agent.remainingDistance < minDistance) {
				GameObject goal;
				do {
					goal = UnityExtensions.GetRandomGameObjectWithTag("FleeTarget");
					print(goal.name);
				} while (safeTarget == goal);

				safeTarget = goal;

				agent.destination = goal.transform.position;
			}
		}
	}
}

public static class UnityExtensions {

	public static GameObject GetRandomGameObjectWithTag(string tag) {
		GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
		return tagged[Random.Range(0, tagged.Length)];
	}

}