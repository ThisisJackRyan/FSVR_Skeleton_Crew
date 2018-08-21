using UnityEngine;
namespace BehaviorDesigner.Runtime.Tasks.ThirdPersonController
{
    [TaskDescription("Sets the character's aim target.")]
    [HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=111")]
    [TaskCategory("Third Person Controller")]
    [TaskIcon("Assets/Behavior Designer/Third Party/Third Person Controller/Editor/Icon.png")]
    [RequiredComponent(typeof(AIAgent))]
    public class SetAimTarget : Action
    {
        [Tooltip("A reference to the agent. If null it will be retrieved from the current GameObject.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The GameObject to aim at")]
        public SharedGameObject aimTarget;

        private GameObject prevTarget;
        private AIAgent aiAgent;

        public override TaskStatus OnUpdate()
        {
            var target = GetDefaultGameObject(targetGameObject.Value);
            if (target != prevTarget) {
                aiAgent = target.GetComponentInParent<AIAgent>();
                prevTarget = target;
            }
            if (aiAgent == null) {
                return TaskStatus.Failure;
            }
            aiAgent.Target = aimTarget.Value;
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            aimTarget = null;
        }
    }
}