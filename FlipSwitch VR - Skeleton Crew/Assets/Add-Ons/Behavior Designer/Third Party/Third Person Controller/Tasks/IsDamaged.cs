using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Did the character get damaged?")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class IsDamaged : Conditional
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The amount of health left")]
        [SharedRequired]
        public SharedFloat currentHealth;
        [Tooltip("The amount of shield left")]
        [SharedRequired]
        public SharedFloat currentShield;
        [SharedRequired]
        public SharedGameObject storeAttacker;

        private GameObject prevTarget;
        private bool damaged = false;

        public override void OnStart()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                if (prevTarget != null) {
                    EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject>(prevTarget, "OnHealthDamageDetails", OnDamage);
                    EventHandler.UnregisterEvent<float>(prevTarget, "OnHealthAmountChange", OnHealthDamage);
                    EventHandler.UnregisterEvent<float>(prevTarget, "OnHealthShieldAmountCHange", OnShieldDamage);
                    EventHandler.UnregisterEvent(target, "OnDeath", OnDeath);
                }

                if (target != null) {
                    EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject>(target, "OnHealthDamageDetails", OnDamage);
                    EventHandler.RegisterEvent<float>(target, "OnHealthAmountChange", OnHealthDamage);
                    EventHandler.RegisterEvent<float>(target, "OnHealthShieldAmountCHange", OnShieldDamage);
                    EventHandler.RegisterEvent(target, "OnDeath", OnDeath);
                }
                prevTarget = target;
            }
        }

        public override TaskStatus OnUpdate()
        {
            return damaged ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnEnd()
        {
            damaged = false;
        }

        private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker)
        {
            damaged = true;
            storeAttacker.Value = attacker;
        }

        private void OnHealthDamage(float amount)
        {
            currentHealth.Value = amount;
        }

        private void OnShieldDamage(float amount)
        {
            currentShield.Value = amount;
        }

        private void OnDeath()
        {
            damaged = false;
        }
    }
}