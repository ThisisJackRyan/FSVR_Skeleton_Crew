using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Tries to stop the use of the item. Can specify a primary or dual wield item.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class StopUse : Action
    {
        public enum UseItemType { Primary, DualWield, Secondary }
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("Specifies the type of item that should be used if itemType is null.")]
        public UseItemType useItemType = UseItemType.Primary;

        private GameObject prevTarget;
        private ItemHandler itemHandler;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                itemHandler = target.GetComponentInParent<ItemHandler>();
                prevTarget = target;
            }
            if (itemHandler == null) {
                return TaskStatus.Failure;
            }
            itemHandler.TryStopUse(useItemType == UseItemType.Primary);
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            useItemType = UseItemType.Primary;
        }
    }
}