using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class SetAttackablePlayers : Action {
	public SharedGameObjectList playerList;

	public override void OnStart() {
		playerList.SetValue( VariableHolder.instance.players );
	}
}
