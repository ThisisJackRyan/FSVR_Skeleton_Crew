using UnityEngine;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// The tracer will show a Line Renderer from the hitscan fire point to the hit point.
    /// </summary>
    public class Tracer : MonoBehaviour
    {
        [Tooltip("The amount of time that the tracer is visible for")]
        [SerializeField] protected float m_VisibleTime = 0.05f;

        // Component references
        private Transform m_Transform;
        private LineRenderer m_LineRenderer;

        /// <summary>
        /// Cache the component references.
        /// </summary>
        private void Awake()
        {
            m_Transform = transform;
            m_LineRenderer = GetComponent<LineRenderer>();
        }

        /// <summary>
        /// Places the object back in the ObjectPool.
        /// </summary>
        private void DestroyObject()
        {
            ObjectPool.Destroy(gameObject);
        }

        /// <summary>
        /// Sets the hit point that the tracer should move to.
        /// </summary>
        /// <param name="hitPoint">The hit point position.</param>
        public void Initialize(Vector3 hitPoint)
        {
            m_LineRenderer.SetPosition(0, m_Transform.position);
            m_LineRenderer.SetPosition(1, hitPoint);

            Scheduler.Schedule(m_VisibleTime, DestroyObject);
        }
    }
}