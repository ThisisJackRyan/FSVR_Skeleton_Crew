using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class FindClosestGameObjectFromList : Action {

    public SharedGameObjectList listToChooseFrom;
	public SharedGameObject selectedTarget;

	public override void OnStart() {

		GameObject[] list = listToChooseFrom.Value.ToArray();
		float dist = float.MaxValue;

		foreach (var go in listToChooseFrom.Value.ToArray()) {
			float temp = Vector3.Distance( transform.position, go.transform.position );
			if (temp < dist) {
				dist = temp;
				selectedTarget.SetValue( go );
			}
		}		
	}
}
