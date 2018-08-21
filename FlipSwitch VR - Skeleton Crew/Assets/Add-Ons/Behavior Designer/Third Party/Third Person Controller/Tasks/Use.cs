using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Tries to use the item. Can specify a primary or secondary item.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class Use : Action
    {
        public enum UseItemType { Primary, DualWield, Secondary }
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The ItemType to use. The item must be equipped. If null the primaryItem value will be used.")]
        public SharedItemType itemType;
        [Tooltip("Specifies the type of item that should be used if itemType is null.")]
        public UseItemType useItemType = UseItemType.Primary;
        [Tooltip("The GameObject to aim at")]
        public SharedGameObject aimTarget;

        private GameObject prevTarget;
        private ItemHandler itemHandler;
        private AIAgent aiAgent;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                itemHandler = target.GetComponentInParent<ItemHandler>();
                aiAgent = target.GetComponentInParent<AIAgent>();
                prevTarget = target;
            }
            if (itemHandler == null) {
                return TaskStatus.Failure;
            }
            if (aiAgent != null) {
                aiAgent.Target = aimTarget.Value;
            }
            if (itemType == null || itemType.Value == null) {
                return itemHandler.TryUseItem(useItemType == UseItemType.Primary ? typeof(PrimaryItemType) : 
                                                (useItemType == UseItemType.DualWield ? typeof(DualWieldItemType) : typeof(SecondaryItemType))) ? TaskStatus.Success : TaskStatus.Failure;
            }
            return itemHandler.TryUseItem(itemType.Value.GetType()) ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            itemType = null;
            useItemType = UseItemType.Primary;
            aimTarget = null;
        }
    }
}