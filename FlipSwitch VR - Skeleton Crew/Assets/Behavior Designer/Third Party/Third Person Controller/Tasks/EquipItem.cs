using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Equips the specified item.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class EquipItem : Conditional
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The ItemType that should be equipped.")]
        public SharedItemType itemType;

        private GameObject prevTarget;
        private Inventory inventory;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                inventory = target.GetComponentInParent<Inventory>();
                prevTarget = target;
            }
            if (inventory == null) {
                return TaskStatus.Failure;
            }
            if (itemType.Value is PrimaryItemType) {
                inventory.EquipItem(itemType.Value as PrimaryItemType);
            } else if (inventory.UnequippedItemType == itemType.Value) {
                // Equip the unequipped item by unequipping hte current item. Make sense?
                inventory.UnequipCurrentItem();
            }
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            itemType = null;
        }
    }
}