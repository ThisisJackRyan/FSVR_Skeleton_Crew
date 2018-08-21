using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Switch the current item.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class SwitchItem : Action
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("Should the primary item be used? If false the secondary item will be used.")]
        public SharedBool primaryItem = true;

        private GameObject prevTarget;
        private InventoryHandler inventoryHandler;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                inventoryHandler = target.GetComponentInParent<InventoryHandler>();
                prevTarget = target;
            }
            if (inventoryHandler == null) {
                return TaskStatus.Failure;
            }
            inventoryHandler.TrySwitchItem(primaryItem.Value, true);
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            primaryItem = true;
        }
    }
}