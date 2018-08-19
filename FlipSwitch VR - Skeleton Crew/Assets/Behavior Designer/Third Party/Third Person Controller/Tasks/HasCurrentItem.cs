using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Does the character currently have the item?")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class HasCurrentItem : Conditional
    {
        public enum ItemType { Primary, Secondary, DualWield }

        [Tooltip("A reference to the GameObject which contains a Inventory. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The type of item to determine if it has any ammo")]
        public ItemType itemType;
        [Tooltip("The item to compare against.")]
        public Item currentItem;

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
            System.Type type = null;
            switch (itemType) {
                case ItemType.Primary:
                    type = typeof(PrimaryItemType);
                    break;
                case ItemType.Secondary:
                    type = typeof(SecondaryItemType);
                    break;
                case ItemType.DualWield:
                    type = typeof(DualWieldItemType);
                    break;
            }
            return inventory.GetCurrentItem(type).Equals(currentItem) ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            itemType = ItemType.Primary;
            currentItem = null;
        }
    }
}