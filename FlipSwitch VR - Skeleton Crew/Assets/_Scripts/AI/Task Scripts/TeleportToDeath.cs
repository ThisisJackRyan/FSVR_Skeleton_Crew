using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;
using System.Collections;
using UnityEngine;

public class TeleportToDeath : Action {

	// Use this for initialization
	public override void OnStart () {

		GetComponent<EnemyDragonkin>().TeleportToDeath();
	}
}
