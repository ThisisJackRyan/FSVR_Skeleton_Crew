using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;

public class EnableNavMeshAgent : Action {

	// Use this for initialization
	public override void OnStart () {
        GetComponent<NavMeshAgent>().enabled = true;
        GetComponent<NavMeshAgent>().autoTraverseOffMeshLink = true;
        GetComponent<NavMeshAgentBridge>().enabled = true;
	}
}
