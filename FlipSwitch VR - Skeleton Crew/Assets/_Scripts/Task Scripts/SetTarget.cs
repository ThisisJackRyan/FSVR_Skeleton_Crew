using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class SelectTarget : Conditional {

	public SharedGameObject selectedTarget;
    public TargetType targetType;

    GameObject target;

    public override void OnStart()
    {
		if ( targetType == TargetType.Cannon ) {
			target = VariableHolder.instance.cannons[Random.Range( 0, VariableHolder.instance.cannons.Count )];
		} else if ( targetType == TargetType.Mast ) {
			target = VariableHolder.instance.mastTargets[Random.Range( 0, VariableHolder.instance.mastTargets.Count )];
		} else if ( targetType == TargetType.Ratmen ) {
			target = VariableHolder.instance.ratmen[Random.Range( 0, VariableHolder.instance.ratmen.Count )];
		} else {
			target = VariableHolder.instance.players[Random.Range( 0, VariableHolder.instance.players.Count )];
		}
    }


    // Use this for initialization
    public override TaskStatus OnUpdate () {
		if ( target == null ) {
			return TaskStatus.Failure;
		} else {
            selectedTarget.SetValue(target);
			return TaskStatus.Success;
		}
	}
}
