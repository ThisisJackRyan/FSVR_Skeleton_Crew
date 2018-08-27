using UnityEngine;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// The Shield inherits from StaticItem to allow for a recoil when hit.
    /// </summary>
    public class Shield : StaticItem
    {
        [Tooltip("The state to play if the melee weapon hits a fixed object")]
        [SerializeField] protected AnimatorItemCollectionData m_RecoilStates = new AnimatorItemCollectionData("Recoil", "Recoil", 0.2f, true);

        // Internal variables
        private bool m_Recoil;
        private int m_RecoilStateIndex;
        
        /// <summary>
        /// Prepare the item for use.
        /// </summary>
        protected override void ItemActivated()
        {
            base.ItemActivated();

            // The shield colliders should always be enabled.
            for (int i = 0; i < m_Colliders.Length; ++i) {
                m_Colliders[i].enabled = true;
                Physics.IgnoreCollision(m_Controller.CapsuleCollider, m_Colliders[i]);
            }

            EventHandler.RegisterEvent(m_Character, "OnAnimatorItemEndRecoil", EndRecoil);
        }

        /// <summary>
        /// The item is no longer equipped.
        /// </summary>
        protected override void ItemDeactivated()
        {
            base.ItemDeactivated();

            // The animation states should begin fresh.
            m_RecoilStates.ResetNextState();

            EventHandler.UnregisterEvent(m_Character, "OnAnimatorItemEndRecoil", EndRecoil);
        }

        /// <summary>
        /// Initializes the item after it has been added to the Inventory.
        /// </summary>
        /// <param name="inventory">The parent character's inventory.</param>
        public override void Init(Inventory inventory)
        {
            base.Init(inventory);

            // Initialize the animation states.
            m_RecoilStates.Initialize(m_ItemType);

            // Determine which hand the item is in for the recoil animation.
            var hand = GetComponentInParent<ItemPlacement>().transform.parent;
            var rightHand = inventory.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            m_RecoilStateIndex = (rightHand == hand ? m_AnimatorMonitor.RightArmLayerIndex : m_AnimatorMonitor.LeftArmLayerIndex);
        }

        /// <summary>
        /// Returns the destination state for the given layer.
        /// </summary>
        /// <param name="priority">Specifies the item animation priority to retrieve. High priority animations get tested before lower priority animations.</param>
        /// <param name="layer">The Animator layer index.</param>
        /// <returns>The state that the Animator should be in for the given layer. A null value indicates no change.</returns>
        public override AnimatorItemStateData GetDestinationState(ItemAnimationPriority priority, int layer)
        {
            var state = base.GetDestinationState(priority, layer);
            if (state != null) {
                return state;
            }

            // Any animation called by the MeleeWeapon component is a high priority animation.
            if (priority == ItemAnimationPriority.High) {
                if (m_Recoil) {
                    state = m_RecoilStates.GetState(layer, m_Controller.Moving);
                    if (state != null) {
                        return state;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// The collider has collided with another object. Perform a recoil animation if the collision object is an item or projectile.
        /// </summary>
        /// <param name="collision">The object that collided with the Shield.</param>
        private void OnCollisionEnter(Collision collision)
        {
            // If the controller is null then the shield hasn't been initialized yet.
            if (m_Controller == null) {
                return;
            }

            // Play the recoil animation if it exists.
            if (collision.rigidbody != null) {
                var recoilState = m_RecoilStates.GetState(m_RecoilStateIndex, m_Controller.Moving);
                if (recoilState != null && !string.IsNullOrEmpty(recoilState.Name)) {
                    m_Recoil = true;
                    EventHandler.ExecuteEvent(m_Character, "OnUpdateAnimator");
                }
            }
        }

        /// <summary>
        /// The recoil animation has ended.
        /// </summary>
        private void EndRecoil()
        {
            if (m_Recoil) {
                m_Recoil = false;
                EventHandler.ExecuteEvent(m_Character, "OnUpdateAnimator");
                m_RecoilStates.ResetNextState();
            }
        }
    }
}