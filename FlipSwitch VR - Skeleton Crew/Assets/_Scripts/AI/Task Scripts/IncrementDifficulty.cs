using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;
using System.Collections;
using UnityEngine;

public class IncrementDifficulty : Action {
	
	// Use this for initialization
	public override void OnStart () {
		GetComponent<EnemyCaptain>().IncrementDifficulty();
	}
}
