using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [System.Serializable]
    public class SharedItemType : SharedVariable<ItemType>
    {
        public static implicit operator SharedItemType(ItemType value) { var sharedVariable = new SharedItemType(); sharedVariable.SetValue(value); return sharedVariable; }
    }
}