using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class EnableNavMeshAgent : Action {

	// Use this for initialization
	public override void OnStart () {
        GetComponent<NavMeshAgent>().enabled = true;	
	}
}
