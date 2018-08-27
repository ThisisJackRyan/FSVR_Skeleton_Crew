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
            target = VariableHolder.instance.cannons.Count > 0 ? VariableHolder.instance.cannons[Random.Range(0, VariableHolder.instance.cannons.Count)] : null;
		} else if ( targetType == TargetType.Mast ) {
            target = VariableHolder.instance.mastTargets.Count > 0 ? VariableHolder.instance.mastTargets[Random.Range(0, VariableHolder.instance.mastTargets.Count)] : null;
        } else if ( targetType == TargetType.Ratmen ) {
            target = VariableHolder.instance.ratmen.Count > 0 ? VariableHolder.instance.ratmen[Random.Range(0, VariableHolder.instance.ratmen.Count)] : null;
        } else {
            target = VariableHolder.instance.players.Count > 0 ? VariableHolder.instance.players[Random.Range(0, VariableHolder.instance.players.Count)] : null;
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
