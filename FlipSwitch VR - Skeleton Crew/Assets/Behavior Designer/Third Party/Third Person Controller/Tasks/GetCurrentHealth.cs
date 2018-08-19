using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Stores the current health and shield?")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class GetCurrentHealth : Conditional
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The amount of health the agent has left")]
        [SharedRequired]
        public SharedFloat storeHealth;
        [Tooltip("The amount of shield the agent has left")]
        [SharedRequired]
        public SharedFloat storeShield;

        private GameObject prevTarget;
        private Health health;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                health = target.GetComponentInParent<Health>();
                prevTarget = target;
            }

            if (health == null) {
                return TaskStatus.Failure;
            }

            storeHealth.Value = health.CurrentHealth;
            storeShield.Value = health.CurrentShield;

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            storeHealth = 0;
            storeShield = 0;
        }
    }
}