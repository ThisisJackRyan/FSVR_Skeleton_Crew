using UnityEngine;
using Opsive.ThirdPersonController;

namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Moves the controller.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    public class Move : Action
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The normalized horizontal movement")]
        public SharedFloat horizontalMovement;
        [Tooltip("The normalized forward movement")]
        public SharedFloat forwardMovement;
        [Tooltip("The forward direction to move")]
        public SharedVector3 direction = Vector3.forward;

        private GameObject prevTarget;
        private RigidbodyCharacterController controller;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                controller = target.GetComponentInParent<RigidbodyCharacterController>();
                prevTarget = target;
            }

            if (controller == null) {
                return TaskStatus.Failure;
            }
            controller.Move(horizontalMovement.Value, forwardMovement.Value, Quaternion.LookRotation(direction.Value));
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            horizontalMovement = 0;
            forwardMovement = 0;
            direction = Vector3.forward;
        }
    }
}