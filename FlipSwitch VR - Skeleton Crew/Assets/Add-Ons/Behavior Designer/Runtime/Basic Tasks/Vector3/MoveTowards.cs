using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks.Basic.UnityVector3
{
    [TaskCategory("Basic/Vector3")]
    [TaskDescription("Move from the current position to the target position.")]
    public class MoveTowards : Action
    {
        [Tooltip("The current position")]
        public SharedVector3 currentPosition;
        [Tooltip("The target position")]
        public SharedVector3 targetPosition;
        [Tooltip("The movement speed")]
        public SharedFloat speed;
        [Tooltip("The move resut")]
        [RequiredField]
        public SharedVector3 storeResult;

        public override TaskStatus OnUpdate() {
            if (transform.position == targetPosition.Value) {
                return TaskStatus.Success;
            }
            // We haven't reached the target yet so keep moving towards it
            transform.position = Vector3.MoveTowards(transform.position, targetPosition.Value, speed.Value * Time.deltaTime);
            return TaskStatus.Running;
           
           // storeResult.Value = Vector3.MoveTowards(currentPosition.Value, targetPosition.Value, speed.Value * Time.deltaTime);
           // return TaskStatus.Success;
        }

        public override void OnReset()
        {
            currentPosition = targetPosition = storeResult = Vector3.zero;
            speed = 0;
        }
    }
}