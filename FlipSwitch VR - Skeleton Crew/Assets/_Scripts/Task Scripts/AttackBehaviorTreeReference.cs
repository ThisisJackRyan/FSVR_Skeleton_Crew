using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;


[TaskIcon("ExternalBehaviorTreeIcon.png")]
public class AttackBehaviorTreeReference : BehaviorTreeReference {

    //[InheritedField]
    public SharedTransform target;
}
