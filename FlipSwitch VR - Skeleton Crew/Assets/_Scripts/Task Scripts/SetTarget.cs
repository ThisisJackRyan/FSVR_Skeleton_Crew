using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class SelectTarget : Conditional {

	public SharedGameObject selectedTarget;
    public TargetType targetType;

    public override void OnStart()
    {
		if ( targetType == TargetType.Cannon ) {
			GameObject randCannon = VariableHolder.instance.cannons[Random.Range( 0, VariableHolder.instance.cannons.Count )];
			selectedTarget.SetValue( randCannon );
		} else if ( targetType == TargetType.Mast ) {
			GameObject randMast = VariableHolder.instance.mastTargets[Random.Range( 0, VariableHolder.instance.mastTargets.Count )];
			selectedTarget.SetValue( randMast );
		} else if ( targetType == TargetType.Ratmen ) {
			GameObject randRat = VariableHolder.instance.ratmen[Random.Range( 0, VariableHolder.instance.ratmen.Count )];
			selectedTarget.SetValue( randRat );
		} else {
			GameObject randPlayer = VariableHolder.instance.players[Random.Range( 0, VariableHolder.instance.players.Count )];
			selectedTarget.SetValue( randPlayer );
		}
    }


    // Use this for initialization
    public override TaskStatus OnUpdate () {
		if ( selectedTarget.Value == null ) {
			return TaskStatus.Failure;
		} else {
			return TaskStatus.Success;
		}
	}
}
