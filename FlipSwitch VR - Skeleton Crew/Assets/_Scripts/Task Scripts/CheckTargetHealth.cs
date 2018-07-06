using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class CheckTargetHealth : Conditional {

    public SharedGameObject currentTarget;
    
    private bool isDead;
	// Use this for initialization
	public override void OnStart () {
        switch (currentTarget.Value.GetComponent<EnemyTargetInit>().targetType)
        {
            case TargetType.Cannon:
				Debug.Log( "accessing cannon health" );
				isDead = !(currentTarget.Value.GetComponent<DamagedObject>().GetHealth() > 0);
                break;
            case TargetType.Player:
				Debug.Log( "accessing player health" );
				isDead = !(currentTarget.Value.GetComponentInParent<ScriptSyncPlayer>().GetHealth() > 0);
                break;
            case TargetType.Ratmen:
				Debug.Log( "accessing ratman health" );
                isDead = !(currentTarget.Value.GetComponent<Ratman>().GetHealth() > 0);
                break;
        }
	}

    // Update is called once per frame
    public override TaskStatus OnUpdate()
    {
        if (isDead)
            return TaskStatus.Success;
       
         return TaskStatus.Failure;
    }
}
