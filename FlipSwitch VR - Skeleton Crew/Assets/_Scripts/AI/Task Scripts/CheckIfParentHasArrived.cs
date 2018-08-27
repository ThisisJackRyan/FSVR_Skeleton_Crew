using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class CheckIfParentHasArrived : Conditional {

    // Use this for initialization
    public override TaskStatus OnUpdate() {
        bool hasArrived = (bool) transform.parent.GetComponent<BehaviorTree>().GetVariable("ShipHasArrived").GetValue();
        if (hasArrived)
            return TaskStatus.Success;

        return TaskStatus.Failure;
    }
}
