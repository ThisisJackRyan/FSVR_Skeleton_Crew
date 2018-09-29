using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.ThirdPersonController.Wrappers;
using UnityEngine;

public class DisableNavMeshAgentBridge : Action {

	public override void OnStart() {
		Debug.Log("disable nav mesh agent bridge started");
		foreach(var nb in transform.root.GetComponentsInChildren<NavMeshAgentBridge>()) {
			Debug.Log("should be disabling the agent bridge");
			nb.enabled = false;
		}
		foreach (var nb in transform.root.GetComponentsInChildren<Opsive.ThirdPersonController.NavMeshAgentBridge>()) {
			Debug.Log("should be disabling the agent bridge");
			nb.enabled = false;
		}
	}
}
