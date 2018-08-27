using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class ToggleWeaponCollider : Action {

    // Use this for initialization
    public override void OnStart()
    {
		GetComponent<Enemy>().ToggleWeaponCollider();
    }
}
