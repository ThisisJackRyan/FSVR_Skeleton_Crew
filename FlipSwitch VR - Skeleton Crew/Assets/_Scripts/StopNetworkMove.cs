using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Networking;


public class StopNetworkMove : NetworkBehaviour {

	

	// Use this for initialization
	void Start () {
		if (!isServer) {
			GetComponent<NavMeshAgent>().updatePosition = false;
			GetComponent<NavMeshAgent>().updateRotation = false;
		}		
	}
	
	// Update is called once per frame
	void Update () {
	}
}
