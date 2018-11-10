using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;
using System.Collections;
using UnityEngine;

public class SelectRandomFromList : Action {


	public SharedGameObjectList possibleTargetList;
    public SharedGameObject selectedTarget;

	// Use this for initialization
	public override void OnStart () {
		selectedTarget.SetValue(possibleTargetList.Value[Random.Range(0, possibleTargetList.Value.Count)]);
	}
}
