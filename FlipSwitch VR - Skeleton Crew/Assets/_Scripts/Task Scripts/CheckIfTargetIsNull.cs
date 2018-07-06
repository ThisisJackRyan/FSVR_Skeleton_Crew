using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class CheckIfTargetIsNull : Conditional {

	public SharedGameObject currentTarget;

	
	// Update is called once per frame
	public override TaskStatus OnUpdate () {
		if (currentTarget.Value == null) {
			return TaskStatus.Success;
		} 

		return TaskStatus.Failure;
		
	}
}
