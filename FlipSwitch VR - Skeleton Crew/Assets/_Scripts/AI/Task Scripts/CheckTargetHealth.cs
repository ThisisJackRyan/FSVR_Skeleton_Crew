﻿using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
//using Sirenix.OdinInspector.Editor.Drawers;
using UnityEngine.AI;

public class CheckTargetHealth : Conditional {

	public SharedGameObject currentTarget;
	
	private bool isDead;
	// Use this for initialization
	public override void OnStart () {
		//Debug.Log("current target is: " + currentTarget.Value.name + " on enemy named " + gameObject.name + " who is dead? " + isDead);
		if (currentTarget.Value) {
			if (currentTarget.Value.GetComponent<EnemyTargetInit>()) {
				switch (currentTarget.Value.GetComponent<EnemyTargetInit>().targetType) {
					case TargetType.Cannon:
						//Debug.Log("accessing cannon health");
						isDead = (currentTarget.Value.GetComponent<DamagedObject>().GetHealth() <= 0);
						break;
					case TargetType.Player:
						//Debug.Log("accessing player health");
						isDead = (currentTarget.Value.GetComponentInParent<Player>().GetHealth() <= 0);
						break;
					case TargetType.Ratmen:
						//Debug.Log("accessing ratman health");
						isDead = (currentTarget.Value.GetComponentInParent<Ratman>().GetHealth() <= 0);
						break;
					default:
						isDead = true;
						break;
				}
			} else {
				Debug.LogWarning(gameObject.name + ": current target is: " + currentTarget.Value.name +  " which does not have an enemyTargetInit component");
			}
		} else {
			Debug.LogWarning(gameObject.name + ": check target health called but no target set.");

		}
	}

	// Update is called once per frame
	public override TaskStatus OnUpdate() {
		return isDead ? TaskStatus.Success : TaskStatus.Failure;
	}
}
