using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Enables or disables the character's aim.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class SetAim : Action
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("Should the item be aimed?")]
        public SharedBool aim = true;
        [Tooltip("The GameObject to aim at")]
        public SharedGameObject aimTarget;

        private GameObject prevTarget;
        private RigidbodyCharacterController controller;
        private AIAgent aiAgent;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                controller = target.GetComponentInParent<RigidbodyCharacterController>();
                aiAgent = target.GetComponentInParent<AIAgent>();
                prevTarget = target;
            }
            if (controller == null) {
                return TaskStatus.Failure;
            }
            controller.Aim = aim.Value;
            if (aiAgent != null) {
                aiAgent.Target = aimTarget.Value;
            }
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            aim = true;
            aimTarget = null;
        }
    }
}